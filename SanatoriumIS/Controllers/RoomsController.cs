using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;
using SanatoriumIS.Models;
using SanatoriumIS.Services;
using SanatoriumIS.ViewModels;

namespace SanatoriumIS.Controllers
{
    [Authorize(Policy = "AdminOrReceptionist")]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoomOccupancyService _roomOccupancyService;

        public RoomsController(ApplicationDbContext context, RoomOccupancyService roomOccupancyService)
        {
            _context = context;
            _roomOccupancyService = roomOccupancyService;
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms.ToListAsync();
            return View(rooms);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.FirstOrDefaultAsync(m => m.Id == id);
            if (room == null) return NotFound();
            return View(room);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Number,Capacity,Category,IsOccupied")] Room room)
        {
            var existingRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.Number == room.Number);
            if (existingRoom != null)
            {
                ModelState.AddModelError("Number", $"Номер '{room.Number}' уже существует.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Number,Capacity,Category,IsOccupied")] Room room)
        {
            if (id != room.Id) return NotFound();

            var existingRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.Number == room.Number && r.Id != room.Id);
            if (existingRoom != null)
            {
                ModelState.AddModelError("Number", $"Номер '{room.Number}' уже существует.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var room = await _context.Rooms.FirstOrDefaultAsync(m => m.Id == id);
            if (room == null) return NotFound();
            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null) _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> OccupancyReport(DateTime? date, bool? showOnlyOccupied)
        {
            var selectedDate = date ?? DateTime.Today;
            var showOnlyOccupiedFlag = showOnlyOccupied ?? false;

            var rooms = await _context.Rooms.ToListAsync();
            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.SecondClient)
                .Where(b => b.CheckIn.Date <= selectedDate.Date && b.CheckOut.Date >= selectedDate.Date)
                .ToListAsync();

            var reportItems = new List<RoomOccupancyItem>();

            foreach (var room in rooms)
            {
                var currentBookings = bookings.Where(b => b.RoomId == room.Id).ToList();
                var isOccupied = currentBookings.Any();

                if (showOnlyOccupiedFlag && !isOccupied) continue;

                var guests = new List<CurrentGuest>();
                foreach (var booking in currentBookings)
                {
                    guests.Add(new CurrentGuest
                    {
                        ClientName = booking.Client?.FullName ?? "Не указан",
                        CheckIn = booking.CheckIn,
                        CheckOut = booking.CheckOut
                    });
                    if (booking.SecondClientId.HasValue && booking.SecondClient != null)
                    {
                        guests.Add(new CurrentGuest
                        {
                            ClientName = booking.SecondClient.FullName,
                            CheckIn = booking.CheckIn,
                            CheckOut = booking.CheckOut
                        });
                    }
                }

                reportItems.Add(new RoomOccupancyItem
                {
                    RoomId = room.Id,
                    RoomNumber = room.Number ?? "---",
                    RoomCategory = room.Category ?? "---",
                    Capacity = room.Capacity,
                    IsOccupied = isOccupied,
                    CurrentGuests = guests
                });
            }

            var viewModel = new RoomOccupancyReportViewModel
            {
                SelectedDate = selectedDate,
                ShowOnlyOccupied = showOnlyOccupiedFlag,
                ReportItems = reportItems.OrderBy(r => r.RoomNumber).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<JsonResult> RefreshOccupancyStatus()
        {
            try
            {
                await _roomOccupancyService.UpdateRoomsOccupancyStatus();
                return Json(new { success = true, message = "Статус номеров обновлён" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Id == id);
        }
    }
}