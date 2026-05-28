using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;
using SanatoriumIS.Models;

namespace SanatoriumIS.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ProcedureRoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProcedureRoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var procedureRooms = await _context.ProcedureRooms.ToListAsync();
            return View(procedureRooms);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var procedureRoom = await _context.ProcedureRooms.FirstOrDefaultAsync(m => m.Id == id);
            if (procedureRoom == null) return NotFound();
            return View(procedureRoom);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,RoomNumber,Name,RoomType,Description")] ProcedureRoom procedureRoom)
        {
            if (await _context.ProcedureRooms.AnyAsync(r => r.RoomNumber == procedureRoom.RoomNumber))
                ModelState.AddModelError("RoomNumber", $"Кабинет с номером '{procedureRoom.RoomNumber}' уже существует.");

            if (ModelState.IsValid)
            {
                _context.Add(procedureRoom);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(procedureRoom);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var procedureRoom = await _context.ProcedureRooms.FindAsync(id);
            if (procedureRoom == null) return NotFound();
            return View(procedureRoom);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RoomNumber,Name,RoomType,Description")] ProcedureRoom procedureRoom)
        {
            if (id != procedureRoom.Id) return NotFound();

            if (await _context.ProcedureRooms.AnyAsync(r => r.RoomNumber == procedureRoom.RoomNumber && r.Id != procedureRoom.Id))
                ModelState.AddModelError("RoomNumber", $"Кабинет с номером '{procedureRoom.RoomNumber}' уже существует.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(procedureRoom);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProcedureRoomExists(procedureRoom.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(procedureRoom);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var procedureRoom = await _context.ProcedureRooms.FirstOrDefaultAsync(m => m.Id == id);
            if (procedureRoom == null) return NotFound();
            return View(procedureRoom);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var procedureRoom = await _context.ProcedureRooms.FindAsync(id);
            if (procedureRoom != null) _context.ProcedureRooms.Remove(procedureRoom);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProcedureRoomExists(int id) => _context.ProcedureRooms.Any(e => e.Id == id);
    }
}