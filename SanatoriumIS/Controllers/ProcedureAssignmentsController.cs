using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;
using SanatoriumIS.Models;
using SanatoriumIS.Services;
using SanatoriumIS.ViewModels;

namespace SanatoriumIS.Controllers
{
    [Authorize(Policy = "CanAssignProcedures")]
    public class ProcedureAssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoomOccupancyService _roomOccupancyService;

        public ProcedureAssignmentsController(ApplicationDbContext context, RoomOccupancyService roomOccupancyService)
        {
            _context = context;
            _roomOccupancyService = roomOccupancyService;
        }

        // GET: ProcedureAssignments
        public async Task<IActionResult> Index(DateTime? date, int? roomId)
        {
            var selectedDate = date ?? DateTime.Today;
            var selectedRoomId = roomId ?? (await _context.ProcedureRooms.FirstOrDefaultAsync())?.Id ?? 0;

            // Получаем клиентов с активным бронированием на выбранную дату (включая второго клиента)
            var activeBookings = await _context.Bookings
                .Where(b => b.CheckIn.Date <= selectedDate.Date && b.CheckOut.Date > selectedDate.Date)
                .ToListAsync();

            var clientIds = new List<int>();

            foreach (var booking in activeBookings)
            {
                clientIds.Add(booking.ClientId);
                if (booking.SecondClientId.HasValue)
                {
                    clientIds.Add(booking.SecondClientId.Value);
                }
            }

            clientIds = clientIds.Distinct().ToList();

            ViewBag.Clients = await _context.Clients
                .Where(c => clientIds.Contains(c.Id))
                .OrderBy(c => c.FullName)
                .Select(c => new { c.Id, c.FullName })
                .ToListAsync();

            var rooms = await _context.ProcedureRooms.ToListAsync();

            var viewModel = new CalendarViewModel
            {
                SelectedDate = selectedDate,
                SelectedRoomId = selectedRoomId,
                Rooms = rooms,
                TimeSlots = new List<TimeSlot>(),
                BookedSlots = new List<BookedSlot>()
            };

            return View(viewModel);
        }

        // GET: ProcedureAssignments/GetSchedule
        [HttpGet]
        public async Task<JsonResult> GetSchedule(int roomId, DateTime date, string? filterType = null, int? clientId = null)
        {
            var assignments = await _context.ProcedureAssignments
                .Include(a => a.Procedure)
                .Include(a => a.Client)
                .Where(a => a.ProcedureRoomId == roomId && a.ProcedureDate.Date == date.Date)
                .ToListAsync();

            var timeSlots = new List<object>();

            for (int hour = 8; hour <= 15; hour++)
            {
                for (int minute = 0; minute < 60; minute += 30)
                {
                    var startTime = new TimeSpan(hour, minute, 0);
                    var endTime = startTime.Add(TimeSpan.FromMinutes(30));
                    var timeKey = startTime.ToString(@"hh\:mm");

                    // Обеденный перерыв 12:00-13:00
                    if (startTime >= new TimeSpan(12, 0, 0) && startTime < new TimeSpan(13, 0, 0))
                    {
                        if (minute == 0)
                        {
                            timeSlots.Add(new
                            {
                                startTime = "12:00",
                                endTime = "13:00",
                                isAvailable = false,
                                isLunch = true,
                                procedureName = "Обеденный перерыв",
                                clientName = "",
                                clientId = 0,
                                assignmentId = 0,
                                duration = 60,
                                status = ""
                            });
                        }
                        continue;
                    }

                    // Находим назначение, которое покрывает этот слот (только НЕ отменённые)
                    var booked = assignments.FirstOrDefault(a => a.StartTime < endTime && a.EndTime > startTime && a.Status != "Отменена");

                    bool slotIsAvailable = booked == null;

                    bool shouldShow = true;

                    if (clientId.HasValue && clientId.Value > 0 && booked != null)
                    {
                        shouldShow = booked.ClientId == clientId.Value;
                    }

                    if (filterType == "busy" && slotIsAvailable) shouldShow = false;
                    if (filterType == "available" && !slotIsAvailable) shouldShow = false;

                    if (!shouldShow) continue;

                    timeSlots.Add(new
                    {
                        startTime = timeKey,
                        endTime = endTime.ToString(@"hh\:mm"),
                        isAvailable = slotIsAvailable,
                        isLunch = false,
                        procedureName = booked?.Procedure?.Name ?? "",
                        clientName = booked?.Client?.FullName ?? "",
                        clientId = booked?.ClientId ?? 0,
                        assignmentId = booked?.Id ?? 0,
                        duration = booked?.Procedure?.DurationMinutes ?? 0,
                        status = booked?.Status ?? ""
                    });
                }
            }

            return Json(new { success = true, timeSlots = timeSlots });
        }

        // GET: ProcedureAssignments/GetAvailableProcedures
        [HttpGet]
        public async Task<JsonResult> GetAvailableProcedures(int roomId, DateTime date, string startTime)
        {
            try
            {
                if (string.IsNullOrEmpty(startTime)) return Json(new { success = false, message = "Выберите время" });
                if (startTime == "12:00") return Json(new { success = false, message = "Нельзя начинать процедуру во время обеда (12:00-13:00)" });

                var room = await _context.ProcedureRooms.FindAsync(roomId);
                if (room == null) return Json(new { success = false, message = "Кабинет не найден" });

                if (!TimeSpan.TryParse(startTime, out var startTimeSpan))
                    return Json(new { success = false, message = "Неверный формат времени" });

                // Проверка начала во время обеда
                if (startTimeSpan >= new TimeSpan(12, 0, 0) && startTimeSpan < new TimeSpan(13, 0, 0))
                    return Json(new { success = false, message = "Нельзя начинать процедуру во время обеда (12:00-13:00)" });

                // Проверяем занятость - учитываем только НЕ отменённые процедуры
                var existingAssignment = await _context.ProcedureAssignments
                    .FirstOrDefaultAsync(a => a.ProcedureRoomId == roomId &&
                        a.ProcedureDate.Date == date.Date &&
                        a.Status != "Отменена" &&
                        a.StartTime <= startTimeSpan &&
                        a.EndTime > startTimeSpan);

                if (existingAssignment != null)
                    return Json(new { success = false, message = "Это время уже занято" });

                var proceduresQuery = _context.Procedures.AsQueryable();
                if (!string.IsNullOrEmpty(room.RoomType))
                    proceduresQuery = proceduresQuery.Where(p => p.RequiredRoomType == null || p.RequiredRoomType == room.RoomType);

                var procedures = await proceduresQuery
                    .Select(p => new { p.Id, p.Name, p.DurationMinutes, p.Price })
                    .ToListAsync();

                if (procedures.Count == 0)
                    return Json(new { success = false, message = "Нет доступных процедур для этого кабинета" });

                return Json(new { success = true, procedures = procedures });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        // GET: ProcedureAssignments/GetClientsWithBooking
        [HttpGet]
        public async Task<JsonResult> GetClientsWithBooking(DateTime date)
        {
            // Получаем клиентов с активным бронированием на выбранную дату (включая второго клиента)
            var activeBookings = await _context.Bookings
                .Where(b => b.CheckIn.Date <= date.Date && b.CheckOut.Date > date.Date)
                .ToListAsync();

            var clientIds = new List<int>();

            foreach (var booking in activeBookings)
            {
                clientIds.Add(booking.ClientId);
                if (booking.SecondClientId.HasValue)
                {
                    clientIds.Add(booking.SecondClientId.Value);
                }
            }

            clientIds = clientIds.Distinct().ToList();

            // Если нет активных бронирований - возвращаем пустой список
            if (clientIds.Count == 0)
            {
                return Json(new { success = true, clients = new List<object>() });
            }

            var clients = await _context.Clients
                .Where(c => clientIds.Contains(c.Id))
                .OrderBy(c => c.FullName)
                .Select(c => new { c.Id, c.FullName })
                .ToListAsync();

            return Json(new { success = true, clients = clients });
        }

        // POST: ProcedureAssignments/CreateAssignment
        [HttpPost]
        public async Task<JsonResult> CreateAssignment([FromBody] CreateAssignmentRequest request)
        {
            try
            {
                if (request == null) return Json(new { success = false, message = "Неверный запрос" });
                if (request.ClientId <= 0) return Json(new { success = false, message = "Выберите клиента" });
                if (request.ProcedureId <= 0) return Json(new { success = false, message = "Выберите процедуру" });
                if (request.RoomId <= 0) return Json(new { success = false, message = "Выберите кабинет" });
                if (request.EmployeeId <= 0) return Json(new { success = false, message = "Для кабинета не назначен сотрудник" });

                var client = await _context.Clients.FindAsync(request.ClientId);
                if (client == null) return Json(new { success = false, message = "Клиент не найден" });

                // Проверка активного бронирования (учитывая первого и второго клиента)
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => (b.ClientId == request.ClientId || b.SecondClientId == request.ClientId) &&
                        b.CheckIn.Date <= request.Date.Date && b.CheckOut.Date > request.Date.Date);

                if (booking == null)
                {
                    return Json(new { success = false, message = "У этого клиента нет активного бронирования на выбранную дату" });
                }

                // Проверка дня выезда
                if (booking.CheckOut.Date == request.Date.Date)
                {
                    return Json(new { success = false, message = "В день выезда процедуры не назначаются" });
                }

                var procedure = await _context.Procedures.FindAsync(request.ProcedureId);
                if (procedure == null) return Json(new { success = false, message = "Процедура не найдена" });

                if (!TimeSpan.TryParse(request.StartTime, out var startTimeSpan))
                    return Json(new { success = false, message = "Неверный формат времени" });

                var endTimeSpan = startTimeSpan.Add(TimeSpan.FromMinutes(procedure.DurationMinutes));

                // Проверка рабочего времени
                if (startTimeSpan < new TimeSpan(8, 0, 0))
                    return Json(new { success = false, message = "Процедуры могут начинаться не раньше 08:00" });
                if (endTimeSpan > new TimeSpan(16, 0, 0))
                    return Json(new { success = false, message = "Процедуры должны заканчиваться не позднее 16:00" });

                // Проверка обеда
                var lunchStart = new TimeSpan(12, 0, 0);
                var lunchEnd = new TimeSpan(13, 0, 0);

                if (startTimeSpan >= lunchStart && startTimeSpan < lunchEnd)
                    return Json(new { success = false, message = "Нельзя начинать процедуру во время обеда (12:00-13:00)" });

                if (startTimeSpan < lunchStart && endTimeSpan > lunchStart)
                    return Json(new { success = false, message = "Процедура не может заканчиваться во время обеда (12:00-13:00). Выберите более раннее время окончания или перенесите на время после обеда (13:00)" });

                if (startTimeSpan < lunchEnd && endTimeSpan > lunchStart)
                    return Json(new { success = false, message = "Процедура не может пересекаться с обеденным перерывом (12:00-13:00)" });

                // Проверка занятости кабинета (игнорируем отменённые)
                var isRoomBusy = await _context.ProcedureAssignments
                    .AnyAsync(a => a.ProcedureRoomId == request.RoomId &&
                        a.ProcedureDate.Date == request.Date.Date &&
                        a.Status != "Отменена" &&
                        a.StartTime < endTimeSpan &&
                        a.EndTime > startTimeSpan);
                if (isRoomBusy) return Json(new { success = false, message = "Кабинет занят" });

                // Проверка занятости сотрудника (игнорируем отменённые)
                var isEmployeeBusy = await _context.ProcedureAssignments
                    .AnyAsync(a => a.EmployeeId == request.EmployeeId &&
                        a.ProcedureDate.Date == request.Date.Date &&
                        a.Status != "Отменена" &&
                        a.StartTime < endTimeSpan &&
                        a.EndTime > startTimeSpan);
                if (isEmployeeBusy) return Json(new { success = false, message = "Сотрудник занят" });

                // Проверка пересечения процедур у клиента (игнорируем отменённые)
                var isClientBusy = await _context.ProcedureAssignments
                    .AnyAsync(a => a.ClientId == request.ClientId &&
                        a.ProcedureDate.Date == request.Date.Date &&
                        a.Status != "Отменена" &&
                        a.StartTime < endTimeSpan &&
                        a.EndTime > startTimeSpan);
                if (isClientBusy) return Json(new { success = false, message = "У клиента уже есть процедура" });

                // Проверка даты трудоустройства сотрудника
                var employee = await _context.Employees.FindAsync(request.EmployeeId);
                if (employee != null && employee.HireDate.Date > request.Date.Date)
                {
                    return Json(new { success = false, message = $"Сотрудник {employee.FullName} принят на работу {employee.HireDate.ToShortDateString()}" });
                }

                var assignment = new ProcedureAssignment
                {
                    ClientId = request.ClientId,
                    ProcedureId = request.ProcedureId,
                    ProcedureRoomId = request.RoomId,
                    EmployeeId = request.EmployeeId,
                    ProcedureDate = request.Date,
                    StartTime = startTimeSpan,
                    EndTime = endTimeSpan,
                    Status = "Запланирована",
                    CreatedAt = DateTime.Now
                };

                _context.ProcedureAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                await _roomOccupancyService.UpdateRoomsOccupancyStatus();

                return Json(new { success = true, message = "Процедура назначена!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateStatus(int assignmentId, string status, string? cancelReason = null)
        {
            // Логируем для отладки
            Console.WriteLine($"=== UpdateStatus called ===");
            Console.WriteLine($"AssignmentId: {assignmentId}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"User: {User.Identity.Name}");
            Console.WriteLine($"IsAuthenticated: {User.Identity.IsAuthenticated}");

            var roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value);
            Console.WriteLine($"Roles: {string.Join(", ", roles)}");

            try
            {
                var assignment = await _context.ProcedureAssignments.FindAsync(assignmentId);
                if (assignment == null)
                {
                    return Json(new { success = false, message = "Назначение не найдено" });
                }

                var currentUser = User.Identity.Name;
                var currentTime = DateTime.Now;

                switch (status)
                {
                    case "Completed":
                        var procedureDateTime = assignment.ProcedureDate.Add(assignment.StartTime);
                        if (procedureDateTime > currentTime)
                        {
                            return Json(new { success = false, message = "Нельзя отметить процедуру как выполненную раньше времени начала" });
                        }

                        assignment.Status = "Выполнена";
                        assignment.CompletedAt = currentTime;
                        assignment.CompletedBy = currentUser;
                        break;

                    case "Cancelled":
                        assignment.Status = "Отменена";
                        assignment.CancelledAt = currentTime;
                        assignment.CancelledBy = currentUser;
                        assignment.CancelReason = cancelReason;
                        break;

                    default:
                        return Json(new { success = false, message = "Неверный статус" });
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Статус изменён на '{status}'" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        // POST: ProcedureAssignments/DeleteAssignment
        [HttpPost]
        [Authorize(Policy = "AdminOrReferringDoctor")]
        public async Task<JsonResult> DeleteAssignment(int assignmentId)
        {
            try
            {
                var assignment = await _context.ProcedureAssignments.FindAsync(assignmentId);
                if (assignment == null)
                {
                    return Json(new { success = false, message = "Назначение не найдено" });
                }

                _context.ProcedureAssignments.Remove(assignment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Назначение удалено!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        // GET: ProcedureAssignments/Report
        [HttpGet]
        public async Task<IActionResult> Report(DateTime? date, int? clientId, string? status)
        {
            var selectedDate = date ?? DateTime.Today;

            // Получаем клиентов ТОЛЬКО с активным бронированием на выбранную дату
            var activeBookings = await _context.Bookings
                .Where(b => b.CheckIn.Date <= selectedDate.Date && b.CheckOut.Date > selectedDate.Date)
                .ToListAsync();

            var clientIds = new List<int>();
            foreach (var booking in activeBookings)
            {
                clientIds.Add(booking.ClientId);
                if (booking.SecondClientId.HasValue)
                    clientIds.Add(booking.SecondClientId.Value);
            }
            clientIds = clientIds.Distinct().ToList();

            var availableClients = await _context.Clients
                .Where(c => clientIds.Contains(c.Id))
                .OrderBy(c => c.FullName)
                .ToListAsync();

            var viewModel = new ProcedureReportViewModel
            {
                SelectedDate = selectedDate,
                SelectedClientId = clientId,
                Clients = availableClients  // ← только клиенты с активным бронированием
            };

            var query = _context.ProcedureAssignments
                .Include(a => a.Client)
                .Include(a => a.Procedure)
                .Include(a => a.ProcedureRoom)
                .Include(a => a.Employee)
                .Where(a => a.ProcedureDate.Date == selectedDate.Date && a.Status == "Выполнена")
                .AsQueryable();

            if (clientId.HasValue && clientId.Value > 0)
                query = query.Where(a => a.ClientId == clientId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            var assignments = await query
                .OrderBy(a => a.StartTime)
                .ThenBy(a => a.Client.FullName)
                .ToListAsync();

            viewModel.ReportItems = assignments.Select(a => new ProcedureReportItem
            {
                Id = a.Id,
                ClientName = a.Client?.FullName ?? "Не указан",
                ProcedureName = a.Procedure?.Name ?? "Не указана",
                RoomName = a.ProcedureRoom?.Name ?? "Не указан",
                EmployeeName = a.Employee?.FullName ?? "Не указан",
                ProcedureDate = a.ProcedureDate,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Status = a.Status ?? "Выполнена"
            }).ToList();

            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Все статусы" },
                new SelectListItem { Value = "Выполнена", Text = "Выполнена" }
            };
            ViewBag.Statuses = statuses;
            ViewBag.SelectedStatus = status;

            return View(viewModel);
        }

        private bool ProcedureAssignmentExists(int id)
        {
            return _context.ProcedureAssignments.Any(e => e.Id == id);
        }
    }

    public class CreateAssignmentRequest
    {
        public int ClientId { get; set; }
        public int ProcedureId { get; set; }
        public int RoomId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public string StartTime { get; set; } = string.Empty;
    }
}