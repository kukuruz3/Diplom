using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;

namespace SanatoriumIS.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (from.HasValue)
                query = query.Where(l => l.Timestamp >= from.Value);
            if (to.HasValue)
                query = query.Where(l => l.Timestamp <= to.Value.AddDays(1));

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(500)
                .ToListAsync();

            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            return View(logs);
        }
    }
}