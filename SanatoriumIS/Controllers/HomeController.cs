using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;
using SanatoriumIS.Models;
using SanatoriumIS.ViewModels;

namespace SanatoriumIS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var totalRooms = await _context.Rooms.CountAsync();
            var occupiedRooms = await _context.Rooms.CountAsync(r => r.IsOccupied == true);
            var availableRooms = totalRooms - occupiedRooms;

            // Динамика бронирований за 7 дней
            var bookingsByDay = new List<object>();
            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(-6 + i);
                var count = await _context.Bookings
                    .CountAsync(b => b.CheckIn.Date <= date && b.CheckOut.Date > date);
                bookingsByDay.Add(new { Date = date.ToString("dd.MM"), Count = count });
            }

            // Динамика процедур за 7 дней
            var proceduresByDay = new List<object>();
            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(-6 + i);
                var count = await _context.ProcedureAssignments
                    .CountAsync(p => p.ProcedureDate.Date == date && p.Status == "Выполнена");
                proceduresByDay.Add(new { Date = date.ToString("dd.MM"), Count = count });
            }

            ViewBag.TotalRooms = totalRooms;
            ViewBag.OccupiedRooms = occupiedRooms;
            ViewBag.AvailableRooms = availableRooms;
            ViewBag.OccupancyPercent = totalRooms > 0 ? (int)((double)occupiedRooms / totalRooms * 100) : 0;
            ViewBag.BookingsByDay = bookingsByDay;
            ViewBag.ProceduresByDay = proceduresByDay;
            ViewBag.TodayProcedures = await _context.ProcedureAssignments
                .CountAsync(p => p.ProcedureDate.Date == today && p.Status == "Выполнена");

            var viewModel = new DashboardViewModel
            {
                ClientsCount = await _context.Clients.CountAsync(),
                RoomsCount = totalRooms,
                BookingsCount = await _context.Bookings.CountAsync(b => !b.IsCheckedOut),
                ProceduresCount = await _context.Procedures.CountAsync(),
                ServicesCount = await _context.Services.CountAsync(),
                FreeRoomsCount = availableRooms,
                TodayBookings = await _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.Room)
                    .Where(b => b.CheckIn.Date <= today && b.CheckOut.Date > today)
                    .Take(5)
                    .ToListAsync(),
                UpcomingBookings = await _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.Room)
                    .Where(b => b.CheckIn.Date > today)
                    .OrderBy(b => b.CheckIn)
                    .Take(5)
                    .ToListAsync(),
                RecentProcedures = await _context.ProcedureAssignments
                    .Include(p => p.Client)
                    .Include(p => p.Procedure)
                    .Where(p => p.ProcedureDate.Date >= today.AddDays(-7) && p.Status == "Выполнена")
                    .OrderByDescending(p => p.ProcedureDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }
    }
}