using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;
using SanatoriumIS.Models;

namespace SanatoriumIS.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ProceduresController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProceduresController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var procedures = await _context.Procedures.ToListAsync();
            return View(procedures);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var procedure = await _context.Procedures.FirstOrDefaultAsync(m => m.Id == id);
            if (procedure == null) return NotFound();
            return View(procedure);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,DurationMinutes,Price,ProcedureType,RequiredRoomType")] Procedure procedure)
        {
            if (ModelState.IsValid)
            {
                _context.Add(procedure);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(procedure);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var procedure = await _context.Procedures.FindAsync(id);
            if (procedure == null) return NotFound();
            return View(procedure);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,DurationMinutes,Price,ProcedureType,RequiredRoomType")] Procedure procedure)
        {
            if (id != procedure.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(procedure);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProcedureExists(procedure.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(procedure);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var procedure = await _context.Procedures.FirstOrDefaultAsync(m => m.Id == id);
            if (procedure == null) return NotFound();
            return View(procedure);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var procedure = await _context.Procedures.FindAsync(id);
            if (procedure != null) _context.Procedures.Remove(procedure);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProcedureExists(int id) => _context.Procedures.Any(e => e.Id == id);
    }
}