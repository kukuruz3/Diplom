using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;
using SanatoriumIS.Models;
using SanatoriumIS.Services;
using System.IO;

namespace SanatoriumIS.Controllers
{
    [Authorize(Policy = "AdminOrReceptionist")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoomOccupancyService _roomOccupancyService;

        public BookingsController(ApplicationDbContext context, RoomOccupancyService roomOccupancyService)
        {
            _context = context;
            _roomOccupancyService = roomOccupancyService;
        }

        // Метод для автоматического выселения просроченных бронирований
        private async Task AutoCheckoutExpiredBookings()
        {
            var now = DateTime.Now;
            var today12pm = new DateTime(now.Year, now.Month, now.Day, 12, 0, 0);

            var expiredBookings = await _context.Bookings
                .Where(b => !b.IsCheckedOut &&
                    (b.CheckOut.Date < now.Date ||
                     (b.CheckOut.Date == now.Date && now >= today12pm)))
                .ToListAsync();

            foreach (var booking in expiredBookings)
            {
                booking.IsCheckedOut = true;
                booking.CheckedOutAt = now;
            }

            if (expiredBookings.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        // GET: Bookings
        public async Task<IActionResult> Index(string searchString)
        {
            await AutoCheckoutExpiredBookings();

            var bookings = _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.SecondClient)
                .Include(b => b.Room)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                bookings = bookings.Where(b =>
                    (b.Client != null && b.Client.FullName.ToLower().Contains(searchString)) ||
                    (b.SecondClient != null && b.SecondClient.FullName.ToLower().Contains(searchString)) ||
                    (b.Room != null && b.Room.Number.ToLower().Contains(searchString)));
            }

            var bookingsList = await bookings.OrderByDescending(b => b.CheckIn).ToListAsync();
            ViewBag.CurrentSearch = searchString;
            return View(bookingsList);
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            await AutoCheckoutExpiredBookings();

            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.SecondClient)
                .Include(b => b.Room)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // GET: Bookings/PrintInvoice/5 (полный расчетный лист)
        public async Task<IActionResult> PrintInvoice(int id)
        {
            await AutoCheckoutExpiredBookings();

            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.SecondClient)
                .Include(b => b.Room)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            var roomPrice = await _context.RoomPrices
                .FirstOrDefaultAsync(rp => rp.Capacity == booking.Room.Capacity && rp.Category == booking.Room.Category);

            ViewBag.RoomPrice = roomPrice?.PricePerNight ?? 0;

            return View(booking);
        }

        // GET: Bookings/PrintProceduresInvoice/5 (только процедуры)
        public async Task<IActionResult> PrintProceduresInvoice(int id, int? clientId = null)
        {
            await AutoCheckoutExpiredBookings();

            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.SecondClient)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            if (booking.Room.Capacity == 2 && !clientId.HasValue)
            {
                var clients = new List<SelectListItem>();
                if (booking.Client != null)
                {
                    clients.Add(new SelectListItem { Value = booking.ClientId.ToString(), Text = booking.Client.FullName });
                }
                if (booking.SecondClient != null)
                {
                    clients.Add(new SelectListItem { Value = booking.SecondClientId.ToString(), Text = booking.SecondClient.FullName });
                }
                ViewBag.Booking = booking;
                ViewBag.Clients = clients;
                return View("SelectClientForInvoice");
            }

            int selectedClientId = clientId ?? booking.ClientId;
            var selectedClient = selectedClientId == booking.ClientId ? booking.Client : booking.SecondClient;

            var procedures = await _context.ProcedureAssignments
                .Include(p => p.Procedure)
                .Where(p => p.ClientId == selectedClientId && p.Status == "Выполнена")
                .ToListAsync();

            ViewBag.SelectedClientId = selectedClientId;
            ViewBag.SelectedClientName = selectedClient?.FullName ?? "Клиент";
            ViewBag.Procedures = procedures;

            return View("PrintProceduresInvoice", booking);
        }

        // POST: Bookings/Checkout/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.IsCheckedOut = true;
            booking.CheckedOutAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Бронирование отмечено как выселенное.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Bookings/Create
        public async Task<IActionResult> Create()
        {
            await LoadViewBags();
            await LoadServices();
            return View();
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ClientId,RoomId,CheckIn,CheckOut,SecondClientId")] Booking booking, int[] selectedServices)
        {
            var room = await _context.Rooms.FindAsync(booking.RoomId);
            bool isDoubleRoom = room != null && room.Capacity == 2;

            if (!isDoubleRoom) booking.SecondClientId = null;
            if (isDoubleRoom && !booking.SecondClientId.HasValue)
                ModelState.AddModelError("SecondClientId", "Для двухместного номера необходимо выбрать второго клиента");
            if (booking.SecondClientId.HasValue && booking.SecondClientId.Value == booking.ClientId)
                ModelState.AddModelError("SecondClientId", "Первый и второй клиент не могут совпадать");

            if (await IsRoomConflict(booking)) ModelState.AddModelError("RoomId", "Этот номер уже забронирован на выбранные даты");
            if (await IsClientConflict(booking.ClientId, booking)) ModelState.AddModelError("ClientId", "У этого клиента уже есть бронирование на выбранные даты");
            if (booking.SecondClientId.HasValue && await IsClientConflict(booking.SecondClientId.Value, booking))
                ModelState.AddModelError("SecondClientId", "У второго клиента уже есть бронирование на выбранные даты");

            if (booking.CheckOut <= booking.CheckIn) ModelState.AddModelError("CheckOut", "Дата выезда должна быть позже даты заезда");
            if ((booking.CheckOut - booking.CheckIn).TotalDays < 1) ModelState.AddModelError("CheckOut", "Минимальный срок бронирования - 1 сутки");
            if ((booking.CheckOut - booking.CheckIn).TotalDays > 30) ModelState.AddModelError("CheckOut", "Максимальный срок бронирования - 30 суток");

            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();

                if (selectedServices != null && selectedServices.Any())
                {
                    foreach (var serviceId in selectedServices)
                    {
                        var service = await _context.Services.FindAsync(serviceId);
                        if (service != null)
                        {
                            var bookingService = new BookingService
                            {
                                BookingId = booking.Id,
                                ServiceId = service.Id,
                                Quantity = 1,
                                PriceAtTime = service.Price,
                                AddedAt = DateTime.Now
                            };
                            _context.BookingServices.Add(bookingService);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                await _roomOccupancyService.UpdateRoomsOccupancyStatus();
                return RedirectToAction(nameof(Index));
            }

            await LoadViewBags(booking);
            await LoadServices(selectedServices);
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.SecondClient)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.SecondClient)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking != null)
            {
                // Удаляем связанные услуги
                var bookingServices = await _context.BookingServices
                    .Where(bs => bs.BookingId == id)
                    .ToListAsync();
                _context.BookingServices.RemoveRange(bookingServices);

                // Удаляем назначения процедур для первого клиента за период бронирования
                var clientProcedures = await _context.ProcedureAssignments
                    .Where(p => p.ClientId == booking.ClientId &&
                                p.ProcedureDate.Date >= booking.CheckIn.Date &&
                                p.ProcedureDate.Date <= booking.CheckOut.Date)
                    .ToListAsync();
                _context.ProcedureAssignments.RemoveRange(clientProcedures);

                // Удаляем назначения процедур для второго клиента (если есть)
                if (booking.SecondClientId.HasValue)
                {
                    var secondClientProcedures = await _context.ProcedureAssignments
                        .Where(p => p.ClientId == booking.SecondClientId.Value &&
                                    p.ProcedureDate.Date >= booking.CheckIn.Date &&
                                    p.ProcedureDate.Date <= booking.CheckOut.Date)
                        .ToListAsync();
                    _context.ProcedureAssignments.RemoveRange(secondClientProcedures);
                }

                // Удаляем само бронирование
                _context.Bookings.Remove(booking);
            }

            await _context.SaveChangesAsync();
            await _roomOccupancyService.UpdateRoomsOccupancyStatus();

            return RedirectToAction(nameof(Index));
        }

        // GET: Bookings/DownloadInvoice/5 - скачать Word файл расчетного листа
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            await AutoCheckoutExpiredBookings();

            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.SecondClient)
                .Include(b => b.Room)
                .Include(b => b.BookingServices)
                    .ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            var roomPrice = await _context.RoomPrices
                .FirstOrDefaultAsync(rp => rp.Capacity == booking.Room.Capacity && rp.Category == booking.Room.Category);

            var nights = (booking.CheckOut - booking.CheckIn).Days;
            var pricePerNight = roomPrice?.PricePerNight ?? 0;
            var stayTotal = pricePerNight * nights;
            var servicesTotal = booking.BookingServices?.Sum(bs => bs.PriceAtTime * bs.Quantity) ?? 0;
            var grandTotal = stayTotal + servicesTotal;
            var isDoubleRoom = booking.Room?.Capacity == 2;
            var roomCapacityText = booking.Room?.Capacity == 1 ? "одноместный" : "двухместный";

            using (var stream = new MemoryStream())
            {
                using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = new Body();

                    // Заголовок
                    body.Append(CreateParagraph("Санаторий \"Нижне-Ивкино\"", true, 24, JustificationValues.Center));
                    body.Append(CreateParagraph($"Расчетный лист № {booking.Id}", true, 20, JustificationValues.Center));
                    body.Append(CreateParagraph($"от {DateTime.Now:dd.MM.yyyy}", false, 16, JustificationValues.Center));
                    body.Append(CreateEmptyParagraph());

                    // Информация о клиенте
                    body.Append(CreateParagraph("Информация о клиенте", true, 18, JustificationValues.Left));
                    if (isDoubleRoom)
                    {
                        body.Append(CreateParagraph($"Первый клиент: {booking.Client?.FullName}", false, 14, JustificationValues.Left));
                        if (booking.SecondClient != null)
                        {
                            body.Append(CreateParagraph($"Второй клиент: {booking.SecondClient.FullName}", false, 14, JustificationValues.Left));
                        }
                    }
                    else
                    {
                        body.Append(CreateParagraph($"Клиент: {booking.Client?.FullName}", false, 14, JustificationValues.Left));
                    }
                    body.Append(CreateParagraph($"Период проживания: {booking.CheckIn:dd.MM.yyyy} — {booking.CheckOut:dd.MM.yyyy}", false, 14, JustificationValues.Left));
                    body.Append(CreateParagraph($"Номер: №{booking.Room?.Number} ({roomCapacityText}, {booking.Room?.Category})", false, 14, JustificationValues.Left));
                    body.Append(CreateEmptyParagraph());

                    // Проживание
                    body.Append(CreateParagraph("Проживание", true, 18, JustificationValues.Left));
                    var stayRow = new[] {
                        $"Проживание в номере ({booking.Room?.Category})",
                        $"{pricePerNight:N0} ₽",
                        nights.ToString(),
                        $"{stayTotal:N0} ₽"
                    };
                    body.Append(CreateTable(new[] { "Услуга", "Цена за ночь", "Кол-во ночей", "Сумма" }, new[] { stayRow }));

                    // Дополнительные услуги
                    if (booking.BookingServices != null && booking.BookingServices.Any())
                    {
                        body.Append(CreateEmptyParagraph());
                        body.Append(CreateParagraph("Дополнительные услуги", true, 18, JustificationValues.Left));
                        var serviceRows = booking.BookingServices.Select(bs => new[] {
                            bs.Service?.Name ?? "",
                            $"{(bs.PriceAtTime * bs.Quantity):N0} ₽"
                        }).ToArray();
                        body.Append(CreateTable(new[] { "Услуга", "Сумма" }, serviceRows));
                    }

                    body.Append(CreateEmptyParagraph());
                    body.Append(CreateParagraph($"ИТОГО К ОПЛАТЕ: {grandTotal:N0} ₽", true, 18, JustificationValues.Right));

                    // Подписи
                    body.Append(CreateEmptyParagraph());
                    body.Append(CreateEmptyParagraph());
                    body.Append(CreateParagraph("_________________", false, 14, JustificationValues.Left));
                    body.Append(CreateParagraph("_________________", false, 14, JustificationValues.Left));
                    body.Append(CreateParagraph("М.П.", false, 14, JustificationValues.Left));

                    mainPart.Document.Append(body);
                    mainPart.Document.Save();
                }

                stream.Position = 0;
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Расчетный_лист_{booking.Id}.docx");
            }
        }

        // GET: Bookings/DownloadProceduresInvoice/5 - скачать Word файл с процедурами
        public async Task<IActionResult> DownloadProceduresInvoice(int id, int? clientId = null)
        {
            await AutoCheckoutExpiredBookings();

            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.SecondClient)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            int selectedClientId;
            string clientName;

            // Если двухместный номер и клиент не выбран - показываем страницу выбора
            if (booking.Room.Capacity == 2 && !clientId.HasValue)
            {
                var clients = new List<SelectListItem>();
                if (booking.Client != null)
                {
                    clients.Add(new SelectListItem { Value = booking.ClientId.ToString(), Text = booking.Client.FullName });
                }
                if (booking.SecondClient != null)
                {
                    clients.Add(new SelectListItem { Value = booking.SecondClientId.ToString(), Text = booking.SecondClient.FullName });
                }
                ViewBag.Booking = booking;
                ViewBag.Clients = clients;
                ViewBag.IsDownload = true;
                return View("SelectClientForInvoice");
            }

            selectedClientId = clientId ?? booking.ClientId;
            clientName = selectedClientId == booking.ClientId ? booking.Client?.FullName : booking.SecondClient?.FullName;

            var procedures = await _context.ProcedureAssignments
                .Include(p => p.Procedure)
                .Where(p => p.ClientId == selectedClientId && p.Status == "Выполнена")
                .ToListAsync();

            var groupedProcedures = procedures
                .GroupBy(p => new { p.ProcedureId, p.Procedure?.Name, p.Procedure?.Price })
                .Select(g => new
                {
                    Name = g.Key.Name ?? "",
                    Price = g.Key.Price ?? 0,
                    Count = g.Count()
                })
                .OrderBy(g => g.Name)
                .ToList();

            var proceduresTotal = procedures.Sum(p => p.Procedure?.Price ?? 0);
            string roomType = booking.Room?.Capacity == 1 ? "одноместный" : "двухместный";

            using (var stream = new MemoryStream())
            {
                using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
                {
                    var mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = new Body();

                    body.Append(CreateParagraph("Санаторий \"Нижне-Ивкино\"", true, 24, JustificationValues.Center));
                    body.Append(CreateParagraph($"Расчетный лист медицинских процедур № {booking.Id}", true, 20, JustificationValues.Center));
                    body.Append(CreateParagraph($"от {DateTime.Now:dd.MM.yyyy}", false, 16, JustificationValues.Center));
                    body.Append(CreateEmptyParagraph());

                    body.Append(CreateParagraph("Информация о клиенте", true, 18, JustificationValues.Left));
                    body.Append(CreateParagraph($"Клиент: {clientName}", false, 14, JustificationValues.Left));
                    body.Append(CreateParagraph($"Период проживания: {booking.CheckIn:dd.MM.yyyy} — {booking.CheckOut:dd.MM.yyyy}", false, 14, JustificationValues.Left));
                    body.Append(CreateParagraph($"Номер: №{booking.Room?.Number} ({roomType}, {booking.Room?.Category})", false, 14, JustificationValues.Left));
                    body.Append(CreateEmptyParagraph());

                    if (groupedProcedures.Any())
                    {
                        body.Append(CreateParagraph("Выполненные медицинские процедуры", true, 18, JustificationValues.Left));
                        var procRows = groupedProcedures.Select(p => new[] {
                    p.Name,
                    p.Count.ToString(),
                    $"{p.Price:N0} ₽",
                    $"{(p.Price * p.Count):N0} ₽"
                }).ToArray();
                        body.Append(CreateTable(new[] { "Процедура", "Кол-во", "Цена за шт.", "Сумма" }, procRows));
                        body.Append(CreateParagraph($"ИТОГО: {proceduresTotal:N0} ₽", true, 16, JustificationValues.Right));
                    }
                    else
                    {
                        body.Append(CreateParagraph("Нет выполненных процедур за период проживания", false, 14, JustificationValues.Left));
                    }

                    body.Append(CreateEmptyParagraph());
                    body.Append(CreateEmptyParagraph());
                    body.Append(CreateParagraph("_________________", false, 14, JustificationValues.Left));
                    body.Append(CreateParagraph("_________________", false, 14, JustificationValues.Left));
                    body.Append(CreateParagraph("М.П.", false, 14, JustificationValues.Left));

                    mainPart.Document.Append(body);
                    mainPart.Document.Save();
                }

                stream.Position = 0;
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"Расчетный_лист_процедур_{booking.Id}_{clientName}.docx");
            }
        }

        // AJAX: Получение всех клиентов
        [HttpGet]
        public async Task<JsonResult> GetAllClients()
        {
            var clients = await _context.Clients
                .OrderBy(c => c.FullName)
                .Select(c => new { c.Id, c.FullName })
                .ToListAsync();
            return Json(clients);
        }

        // AJAX: Поиск по всем клиентам
        [HttpGet]
        public async Task<JsonResult> SearchAllClients(string term)
        {
            var query = _context.Clients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(c => c.FullName.Contains(term));
            }
            var clients = await query
                .OrderBy(c => c.FullName)
                .Select(c => new { c.Id, c.FullName })
                .ToListAsync();
            return Json(clients);
        }

        // AJAX: Получение доступных номеров
        [HttpGet]
        public async Task<JsonResult> GetAvailableRooms(DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var bookedRoomIds = await _context.Bookings
                    .Where(b => b.CheckIn < checkOut && b.CheckOut > checkIn)
                    .Select(b => b.RoomId)
                    .Distinct()
                    .ToListAsync();

                var availableRooms = await _context.Rooms
                    .Where(r => !bookedRoomIds.Contains(r.Id))
                    .Select(r => new { r.Id, r.Number, r.Capacity })
                    .ToListAsync();

                return Json(new { success = true, rooms = availableRooms });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // AJAX: Получение доступных клиентов для выбранных дат
        [HttpGet]
        public async Task<JsonResult> GetAvailableClientsForDates(DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var activeBookings = await _context.Bookings
                    .Where(b => b.CheckIn < checkOut && b.CheckOut > checkIn)
                    .ToListAsync();

                var busyClientIds = new List<int>();
                foreach (var booking in activeBookings)
                {
                    busyClientIds.Add(booking.ClientId);
                    if (booking.SecondClientId.HasValue)
                    {
                        busyClientIds.Add(booking.SecondClientId.Value);
                    }
                }
                busyClientIds = busyClientIds.Distinct().ToList();

                var availableClients = await _context.Clients
                    .Where(c => !busyClientIds.Contains(c.Id))
                    .OrderBy(c => c.FullName)
                    .Select(c => new { c.Id, c.FullName })
                    .ToListAsync();

                return Json(new { success = true, clients = availableClients });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // AJAX: Получение доступных вторых клиентов
        [HttpGet]
        public async Task<JsonResult> GetAvailableSecondClients(DateTime checkIn, DateTime checkOut, int firstClientId)
        {
            try
            {
                var activeBookings = await _context.Bookings
                    .Where(b => b.CheckIn < checkOut && b.CheckOut > checkIn)
                    .ToListAsync();

                var busyClientIds = new List<int>();
                foreach (var booking in activeBookings)
                {
                    busyClientIds.Add(booking.ClientId);
                    if (booking.SecondClientId.HasValue)
                    {
                        busyClientIds.Add(booking.SecondClientId.Value);
                    }
                }
                busyClientIds = busyClientIds.Distinct().ToList();

                if (firstClientId > 0)
                {
                    busyClientIds.Add(firstClientId);
                }

                var availableClients = await _context.Clients
                    .Where(c => !busyClientIds.Contains(c.Id))
                    .OrderBy(c => c.FullName)
                    .Select(c => new { c.Id, c.FullName })
                    .ToListAsync();

                return Json(new { success = true, clients = availableClients });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // AJAX: Поиск клиентов для Create (только свободные)
        [HttpGet]
        public async Task<JsonResult> SearchClients(string term, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var activeBookings = await _context.Bookings
                    .Where(b => b.CheckIn < checkOut && b.CheckOut > checkIn)
                    .ToListAsync();

                var busyClientIds = new List<int>();
                foreach (var booking in activeBookings)
                {
                    busyClientIds.Add(booking.ClientId);
                    if (booking.SecondClientId.HasValue)
                    {
                        busyClientIds.Add(booking.SecondClientId.Value);
                    }
                }
                busyClientIds = busyClientIds.Distinct().ToList();

                var query = _context.Clients
                    .Where(c => !busyClientIds.Contains(c.Id))
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    query = query.Where(c => c.FullName.Contains(term));
                }

                var clients = await query
                    .OrderBy(c => c.FullName)
                    .Select(c => new { c.Id, c.FullName })
                    .ToListAsync();

                return Json(clients);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // AJAX: Поиск клиентов для Edit
        [HttpGet]
        public async Task<JsonResult> SearchClientsForEdit(string term, int? currentClientId, bool isSecond = false, DateTime? checkIn = null, DateTime? checkOut = null, int? currentBookingId = null)
        {
            var searchCheckIn = checkIn ?? DateTime.Today;
            var searchCheckOut = checkOut ?? DateTime.Today.AddDays(1);

            var activeBookings = await _context.Bookings
                .Where(b => b.CheckIn < searchCheckOut && b.CheckOut > searchCheckIn)
                .ToListAsync();

            if (currentBookingId.HasValue)
            {
                activeBookings = activeBookings.Where(b => b.Id != currentBookingId.Value).ToList();
            }

            var busyClientIds = new List<int>();
            foreach (var booking in activeBookings)
            {
                busyClientIds.Add(booking.ClientId);
                if (booking.SecondClientId.HasValue)
                {
                    busyClientIds.Add(booking.SecondClientId.Value);
                }
            }
            busyClientIds = busyClientIds.Distinct().ToList();

            var query = _context.Clients
                .Where(c => !busyClientIds.Contains(c.Id) || c.Id == currentClientId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(c => c.FullName.Contains(term));
            }

            if (isSecond && currentClientId.HasValue)
            {
                var clients = await query
                    .Where(c => c.Id != currentClientId.Value)
                    .OrderBy(c => c.FullName)
                    .Select(c => new { c.Id, c.FullName })
                    .ToListAsync();
                return Json(clients);
            }

            var allClients = await query
                .OrderBy(c => c.FullName)
                .Select(c => new { c.Id, c.FullName })
                .ToListAsync();

            return Json(allClients);
        }

        // AJAX: Получение клиента по ID
        [HttpGet]
        public async Task<JsonResult> GetClient(int id)
        {
            var client = await _context.Clients
                .Where(c => c.Id == id)
                .Select(c => new { c.Id, c.FullName, c.Phone })
                .FirstOrDefaultAsync();
            return Json(client);
        }

        // ================================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ================================================================

        private async Task LoadViewBags(Booking? booking = null)
        {
            var rooms = await _context.Rooms
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"{r.Number} ({(r.Capacity == 1 ? "одноместный" : "двухместный")}) - {r.Category}"
                })
                .ToListAsync();

            var clients = await _context.Clients
                .OrderBy(c => c.FullName)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.FullName
                })
                .ToListAsync();

            ViewBag.RoomId = new SelectList(rooms, "Value", "Text", booking?.RoomId);
            ViewBag.ClientId = new SelectList(clients, "Value", "Text", booking?.ClientId);
            ViewBag.SecondClientId = new SelectList(clients, "Value", "Text", booking?.SecondClientId);
        }

        private async Task LoadServices(int[]? selectedServices = null)
        {
            var services = await _context.Services.ToListAsync();
            ViewBag.Services = services;
            ViewBag.SelectedServices = selectedServices ?? new int[0];
        }

        private async Task<bool> IsRoomConflict(Booking booking, int? excludeId = null)
        {
            return await _context.Bookings.AnyAsync(b => b.RoomId == booking.RoomId && b.Id != excludeId &&
                b.CheckIn < booking.CheckOut && b.CheckOut > booking.CheckIn);
        }

        private async Task<bool> IsClientConflict(int clientId, Booking booking, int? excludeId = null)
        {
            return await _context.Bookings.AnyAsync(b => (b.ClientId == clientId || b.SecondClientId == clientId) && b.Id != excludeId &&
                b.CheckIn < booking.CheckOut && b.CheckOut > booking.CheckIn);
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }

        // ================================================================
        // МЕТОДЫ ДЛЯ СОЗДАНИЯ WORD ДОКУМЕНТОВ
        // ================================================================

        private Paragraph CreateParagraph(string text, bool bold, int fontSize, JustificationValues alignment)
        {
            var run = new Run();
            if (bold)
            {
                run.Append(new Bold());
            }
            run.Append(new RunProperties(new FontSize() { Val = (fontSize * 2).ToString() }));
            run.Append(new Text(text));

            var paragraph = new Paragraph(run);
            paragraph.ParagraphProperties = new ParagraphProperties(new Justification() { Val = alignment });

            return paragraph;
        }

        private Paragraph CreateEmptyParagraph()
        {
            var paragraph = new Paragraph();
            paragraph.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Left });
            return paragraph;
        }

        private Table CreateTable(string[] headers, string[][] rows)
        {
            var table = new Table();

            var tableProperties = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 1 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 1 },
                    new LeftBorder() { Val = BorderValues.Single, Size = 1 },
                    new RightBorder() { Val = BorderValues.Single, Size = 1 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 1 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 1 }
                ),
                new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
            );
            table.Append(tableProperties);

            // Заголовки
            var headerRow = new TableRow();
            foreach (var header in headers)
            {
                var run = new Run();
                run.Append(new Bold());
                run.Append(new Text(header));
                var paragraph = new Paragraph(run);
                paragraph.ParagraphProperties = new ParagraphProperties(new Justification() { Val = JustificationValues.Center });

                var cell = new TableCell(paragraph);
                headerRow.Append(cell);
            }
            table.Append(headerRow);

            // Данные
            foreach (var row in rows)
            {
                var dataRow = new TableRow();
                foreach (var cellText in row)
                {
                    var paragraph = new Paragraph(new Run(new Text(cellText)));
                    var cell = new TableCell(paragraph);
                    dataRow.Append(cell);
                }
                table.Append(dataRow);
            }

            return table;
        }
    }
}