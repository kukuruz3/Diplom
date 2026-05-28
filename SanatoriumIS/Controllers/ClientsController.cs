using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;
using SanatoriumIS.Models;

namespace SanatoriumIS.Controllers
{
    [Authorize(Policy = "AdminOrReceptionist")]
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var clients = await _context.Clients.ToListAsync();
            return View(clients);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var client = await _context.Clients.FirstOrDefaultAsync(m => m.Id == id);
            if (client == null) return NotFound();
            return View(client);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FullName,PassportRaw,Phone,BirthDate")] Client client)
        {
            if (!string.IsNullOrEmpty(client.PassportRaw))
            {
                string cleanPassport = client.PassportRaw.Replace(" ", "");
                client.PassportHash = BCrypt.Net.BCrypt.HashPassword(cleanPassport);
            }
            client.PassportRaw = null;

            if (ModelState.IsValid)
            {
                _context.Add(client);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,PassportRaw,Phone,BirthDate")] Client client)
        {
            if (id != client.Id) return NotFound();

            var existingClient = await _context.Clients.FindAsync(id);
            if (existingClient == null) return NotFound();

            existingClient.FullName = client.FullName;
            existingClient.Phone = client.Phone;
            existingClient.BirthDate = client.BirthDate;

            if (!string.IsNullOrEmpty(client.PassportRaw))
            {
                string cleanPassport = client.PassportRaw.Replace(" ", "");
                existingClient.PassportHash = BCrypt.Net.BCrypt.HashPassword(cleanPassport);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(existingClient);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(existingClient.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var client = await _context.Clients.FirstOrDefaultAsync(m => m.Id == id);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null) _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.Id == id);
        }
    }
}