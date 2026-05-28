using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;
using SanatoriumIS.Models;

namespace SanatoriumIS.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class RoomPricesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomPricesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var prices = await _context.RoomPrices.OrderBy(p => p.Capacity).ThenBy(p => p.Category).ToListAsync();
            return View(prices);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var roomPrice = await _context.RoomPrices.FirstOrDefaultAsync(m => m.Id == id);
            if (roomPrice == null) return NotFound();
            return View(roomPrice);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Capacity,Category,PricePerNight,Description")] RoomPrice roomPrice)
        {
            if (await _context.RoomPrices.AnyAsync(p => p.Capacity == roomPrice.Capacity && p.Category == roomPrice.Category))
                ModelState.AddModelError("", $"Цена для {roomPrice.Capacity}-местного номера категории '{roomPrice.Category}' уже существует.");

            if (ModelState.IsValid)
            {
                _context.Add(roomPrice);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(roomPrice);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var roomPrice = await _context.RoomPrices.FindAsync(id);
            if (roomPrice == null) return NotFound();
            return View(roomPrice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Capacity,Category,PricePerNight,Description")] RoomPrice roomPrice)
        {
            if (id != roomPrice.Id) return NotFound();

            if (await _context.RoomPrices.AnyAsync(p => p.Capacity == roomPrice.Capacity && p.Category == roomPrice.Category && p.Id != roomPrice.Id))
                ModelState.AddModelError("", $"Цена для {roomPrice.Capacity}-местного номера категории '{roomPrice.Category}' уже существует.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(roomPrice);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomPriceExists(roomPrice.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(roomPrice);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var roomPrice = await _context.RoomPrices.FirstOrDefaultAsync(m => m.Id == id);
            if (roomPrice == null) return NotFound();
            return View(roomPrice);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var roomPrice = await _context.RoomPrices.FindAsync(id);
            if (roomPrice != null) _context.RoomPrices.Remove(roomPrice);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetPrice(int capacity, string category)
        {
            var price = await _context.RoomPrices.FirstOrDefaultAsync(p => p.Capacity == capacity && p.Category == category);
            if (price != null) return Json(new { success = true, price = price.PricePerNight });
            return Json(new { success = false, price = 0 });
        }

        private bool RoomPriceExists(int id) => _context.RoomPrices.Any(e => e.Id == id);
    }
}