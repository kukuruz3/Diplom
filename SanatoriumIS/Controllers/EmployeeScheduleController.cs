using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;
using SanatoriumIS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SanatoriumIS.Controllers
{
    [Authorize]
    public class EmployeeScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");

            var employees = await _context.Employees
                .Include(e => e.ProcedureRoom)
                .Where(e => e.IsActive)
                .ToListAsync();

            var assignments = await _context.ProcedureAssignments
                .Include(a => a.Client)
                .Include(a => a.Procedure)
                .Include(a => a.Employee)
                .Where(a => a.ProcedureDate.Date == selectedDate.Date)
                .ToListAsync();

            ViewBag.Employees = employees;
            ViewBag.Assignments = assignments;

            return View();
        }
    }
}