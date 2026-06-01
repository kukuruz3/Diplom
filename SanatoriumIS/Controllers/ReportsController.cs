using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;
using SanatoriumIS.Models;
using SanatoriumIS.ViewModels;

namespace SanatoriumIS.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reports/ProcedureRanking
        public async Task<IActionResult> ProcedureRanking(DateTime? dateFrom, DateTime? dateTo)
        {
            var from = dateFrom ?? DateTime.Today.AddMonths(-1);
            var to = dateTo ?? DateTime.Today;

            var procedureStats = await _context.ProcedureAssignments
                .Include(p => p.Procedure)
                .Where(p => p.ProcedureDate.Date >= from.Date && p.ProcedureDate.Date <= to.Date && p.Status == "Выполнена")
                .GroupBy(p => new { p.ProcedureId, p.Procedure.Name, p.Procedure.Price })
                .Select(g => new
                {
                    ProcedureId = g.Key.ProcedureId,
                    ProcedureName = g.Key.Name ?? "Неизвестно",
                    Count = g.Count(),
                    TotalRevenue = g.Count() * g.Key.Price,
                    PricePerProcedure = g.Key.Price
                })
                .OrderByDescending(r => r.Count)
                .ToListAsync();

            var model = procedureStats.Cast<object>().ToList();

            ViewBag.DateFrom = from.ToString("yyyy-MM-dd");
            ViewBag.DateTo = to.ToString("yyyy-MM-dd");
            ViewBag.TotalProcedures = procedureStats.Sum(p => p.Count);
            ViewBag.TotalRevenue = procedureStats.Sum(p => p.TotalRevenue);

            return View(model);
        }

        // GET: Reports/OccupancyForecast
        public async Task<IActionResult> OccupancyForecast()
        {
            var forecast = new List<object>();
            var today = DateTime.Today;

            for (int i = 0; i < 30; i++)
            {
                var date = today.AddDays(i);
                var bookingsCount = await _context.Bookings
                    .CountAsync(b => b.CheckIn.Date <= date && b.CheckOut.Date > date);

                var totalRooms = await _context.Rooms.CountAsync();
                var occupancyPercent = totalRooms > 0 ? (int)((double)bookingsCount / totalRooms * 100) : 0;

                forecast.Add(new
                {
                    Date = date,
                    BookedRooms = bookingsCount,
                    TotalRooms = totalRooms,
                    OccupancyPercent = occupancyPercent
                });
            }

            return View(forecast);
        }

        // GET: Reports/CancelledProcedures
        public async Task<IActionResult> CancelledProcedures(DateTime? dateFrom, DateTime? dateTo)
        {
            var from = dateFrom ?? DateTime.Today.AddMonths(-3);
            var to = dateTo ?? DateTime.Today;

            var cancelledProcedures = await _context.ProcedureAssignments
                .Include(p => p.Client)
                .Include(p => p.Procedure)
                .Include(p => p.Employee)
                .Where(p => p.Status == "Отменена" &&
                            p.CancelledAt.HasValue &&
                            p.CancelledAt.Value.Date >= from.Date &&
                            p.CancelledAt.Value.Date <= to.Date)
                .OrderByDescending(p => p.CancelledAt)
                .ToListAsync();

            var modelList = new List<CancelledProcedureViewModel>();

            foreach (var p in cancelledProcedures)
            {
                // Получаем имя отменившего
                string cancelledByName = "Неизвестно";

                if (!string.IsNullOrEmpty(p.CancelledBy))
                {
                    // Сначала ищем среди сотрудников
                    var employee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.IdentityUserId == p.CancelledBy);

                    if (employee != null)
                    {
                        cancelledByName = employee.FullName;
                    }
                    else
                    {
                        // Ищем среди пользователей
                        var user = await _userManager.FindByIdAsync(p.CancelledBy);
                        if (user != null)
                        {
                            cancelledByName = user.FullName ?? user.Email ?? p.CancelledBy;
                        }
                        else
                        {
                            cancelledByName = p.CancelledBy;
                        }
                    }
                }

                modelList.Add(new CancelledProcedureViewModel
                {
                    Id = p.Id,
                    ClientName = p.Client?.FullName ?? "Не указан",
                    ProcedureName = p.Procedure?.Name ?? "Не указана",
                    EmployeeName = p.Employee?.FullName ?? "Не указан",
                    CancelledDate = p.CancelledAt ?? DateTime.Now,
                    CancelReason = p.CancelReason ?? "Не указана",
                    CancelledByName = cancelledByName,
                    OriginalDate = p.ProcedureDate,
                    OriginalTime = p.StartTime
                });
            }

            // Статистика по отменам
            var byEmployeeStats = modelList
                .Where(p => p.CancelledByName != "Неизвестно")
                .GroupBy(p => p.CancelledByName)
                .Select(g => new { EmployeeName = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            ViewBag.DateFrom = from.ToString("yyyy-MM-dd");
            ViewBag.DateTo = to.ToString("yyyy-MM-dd");
            ViewBag.TotalCancelled = cancelledProcedures.Count;
            ViewBag.ByEmployeeStats = byEmployeeStats;

            return View(modelList);
        }
    }
}