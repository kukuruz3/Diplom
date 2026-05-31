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
            var viewModel = new DashboardViewModel
            {
                ClientsCount = await _context.Clients.CountAsync(),
                RoomsCount = await _context.Rooms.CountAsync(),
                BookingsCount = await _context.Bookings.CountAsync(),
                ProceduresCount = await _context.Procedures.CountAsync(),
                ServicesCount = await _context.Services.CountAsync(),
                FreeRoomsCount = await _context.Rooms.CountAsync(r => !r.IsOccupied),
                TodayBookings = await _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.Room)
                    .Where(b => b.CheckIn.Date <= DateTime.Today && b.CheckOut.Date >= DateTime.Today)
                    .Take(5)
                    .ToListAsync(),
                UpcomingBookings = await _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.Room)
                    .Where(b => b.CheckIn.Date > DateTime.Today)
                    .OrderBy(b => b.CheckIn)
                    .Take(5)
                    .ToListAsync(),
                RecentProcedures = await _context.ProcedureAssignments
                    .Include(p => p.Client)
                    .Include(p => p.Procedure)
                    .Include(p => p.ProcedureRoom)
                    .Where(p => p.ProcedureDate.Date >= DateTime.Today && p.Status == "Выполнена")
                    .OrderBy(p => p.ProcedureDate)
                    .ThenBy(p => p.StartTime)
                    .Take(5)
                    .ToListAsync()
            };

            ViewBag.TodayProcedures = await _context.ProcedureAssignments
                .CountAsync(p => p.ProcedureDate.Date == DateTime.Today && p.Status == "Выполнена");

            return View(viewModel);
        }
    }
}