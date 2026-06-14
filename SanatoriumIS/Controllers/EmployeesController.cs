using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;
using SanatoriumIS.Models;
using System.Globalization;

namespace SanatoriumIS.Controllers
{
    [Authorize(Policy = "AdminOrMedicalStaff")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmployeesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Employees (только для Admin)
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Index(bool? showPassword, string? password, string? employeeName, string? employeeEmail, string? employeeRole)
        {
            var employees = await _context.Employees
                .Include(e => e.ProcedureRoom)
                .Include(e => e.IdentityUser)
                .ToListAsync();

            // Если есть параметры для показа пароля
            if (showPassword == true && !string.IsNullOrEmpty(password))
            {
                ViewBag.ShowPasswordModal = true;
                ViewBag.NewPassword = password;
                ViewBag.NewEmployeeName = employeeName;
                ViewBag.NewEmployeeEmail = employeeEmail;
                ViewBag.NewEmployeeRole = employeeRole;
            }

            return View(employees);
        }

        // GET: Employees/Details/5 (только для Admin)
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.ProcedureRoom)
                .Include(e => e.IdentityUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // GET: Employees/Create (только для Admin)
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create()
        {
            var positions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Главный врач", Text = "Главный врач" },
                new SelectListItem { Value = "Врач терапевт", Text = "Врач терапевт" },
                new SelectListItem { Value = "Врач физиотерапевт", Text = "Врач физиотерапевт" },
                new SelectListItem { Value = "Врач реабилитолог", Text = "Врач реабилитолог" },
                new SelectListItem { Value = "Массажист", Text = "Массажист" },
                new SelectListItem { Value = "Инструктор ЛФК", Text = "Инструктор ЛФК" },
                new SelectListItem { Value = "Регистратор", Text = "Регистратор" },
                new SelectListItem { Value = "Администратор", Text = "Администратор" }
            };
            ViewBag.Positions = new SelectList(positions, "Value", "Text");

            var specializations = new List<SelectListItem>
            {
                new SelectListItem { Value = "Терапия", Text = "Терапия" },
                new SelectListItem { Value = "Физиотерапия", Text = "Физиотерапия" },
                new SelectListItem { Value = "Массаж", Text = "Массаж" },
                new SelectListItem { Value = "ЛФК", Text = "ЛФК" },
                new SelectListItem { Value = "Реабилитология", Text = "Реабилитология" }
            };
            ViewBag.Specializations = new SelectList(specializations, "Value", "Text");

            var rooms = await _context.ProcedureRooms
                .Select(r => new
                {
                    r.Id,
                    DisplayName = $"№{r.RoomNumber} - {r.Name}"
                })
                .ToListAsync();
            ViewBag.ProcedureRoomId = new SelectList(rooms, "Id", "DisplayName");

            var roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Admin", Text = "Администратор" },
                new SelectListItem { Value = "Receptionist", Text = "Регистратор" },
                new SelectListItem { Value = "ReferringDoctor", Text = "Врач-терапевт (назначающий)" },
                new SelectListItem { Value = "ExecutingDoctor", Text = "Врач-специалист (исполняющий)" }
            };
            ViewBag.SystemRoles = new SelectList(roles, "Value", "Text");

            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([Bind("Id,FullName,Position,Specialization,Phone,Email,ProcedureRoomId,HireDate,Salary,IsActive,HasSystemAccess,SystemRole")] Employee employee)
        {
            // Обработка зарплаты
            if (Request.Form.ContainsKey("Salary"))
            {
                var salaryStr = Request.Form["Salary"].ToString().Trim();
                if (string.IsNullOrEmpty(salaryStr))
                {
                    ModelState.AddModelError("Salary", "Поле Зарплата обязательно для заполнения");
                }
                else
                {
                    salaryStr = System.Text.RegularExpressions.Regex.Replace(salaryStr, @"[^\d,.]", "").Replace(",", ".");
                    ModelState.Remove("Salary");
                    if (decimal.TryParse(salaryStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal salaryValue))
                    {
                        employee.Salary = salaryValue;
                    }
                    else
                    {
                        ModelState.AddModelError("Salary", "Введите корректное число (например: 50000 или 50000.50)");
                    }
                }
            }

            // ПРОВЕРКА: если включён доступ в систему, то Email и SystemRole обязательны
            if (employee.HasSystemAccess)
            {
                if (string.IsNullOrWhiteSpace(employee.Email))
                {
                    ModelState.AddModelError("Email", "Для доступа в систему необходимо указать Email");
                }
                if (string.IsNullOrWhiteSpace(employee.SystemRole))
                {
                    ModelState.AddModelError("SystemRole", "Для доступа в систему необходимо выбрать роль");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();

                string tempPassword = null;

                // Если сотрудник имеет доступ в систему - создаём учётную запись
                if (employee.HasSystemAccess && !string.IsNullOrEmpty(employee.Email) && !string.IsNullOrEmpty(employee.SystemRole))
                {
                    tempPassword = await CreateUserAccount(employee);
                }

                if (tempPassword != null)
                {
                    TempData["ShowPasswordModal"] = "true";
                    TempData["NewPassword"] = tempPassword;
                    TempData["NewEmployeeName"] = employee.FullName;
                    TempData["NewEmployeeEmail"] = employee.Email;
                    TempData["NewEmployeeRole"] = employee.SystemRole;
                }
                else
                {
                    TempData["SuccessMessage"] = $"Сотрудник {employee.FullName} создан.";
                }

                return RedirectToAction(nameof(Index));
            }

            // Перезагружаем ViewBag при ошибке
            var positions = new List<SelectListItem>
    {
        new SelectListItem { Value = "Главный врач", Text = "Главный врач" },
        new SelectListItem { Value = "Врач терапевт", Text = "Врач терапевт" },
        new SelectListItem { Value = "Врач физиотерапевт", Text = "Врач физиотерапевт" },
        new SelectListItem { Value = "Врач реабилитолог", Text = "Врач реабилитолог" },
        new SelectListItem { Value = "Массажист", Text = "Массажист" },
        new SelectListItem { Value = "Инструктор ЛФК", Text = "Инструктор ЛФК" },
        new SelectListItem { Value = "Регистратор", Text = "Регистратор" },
        new SelectListItem { Value = "Администратор", Text = "Администратор" }
    };
            ViewBag.Positions = new SelectList(positions, "Value", "Text");

            var specializations = new List<SelectListItem>
    {
        new SelectListItem { Value = "Терапия", Text = "Терапия" },
        new SelectListItem { Value = "Физиотерапия", Text = "Физиотерапия" },
        new SelectListItem { Value = "Массаж", Text = "Массаж" },
        new SelectListItem { Value = "ЛФК", Text = "ЛФК" },
        new SelectListItem { Value = "Реабилитология", Text = "Реабилитология" }
    };
            ViewBag.Specializations = new SelectList(specializations, "Value", "Text");

            var rooms = await _context.ProcedureRooms
                .Select(r => new
                {
                    r.Id,
                    DisplayName = $"№{r.RoomNumber} - {r.Name}"
                })
                .ToListAsync();
            ViewBag.ProcedureRoomId = new SelectList(rooms, "Id", "DisplayName", employee.ProcedureRoomId);

            var roles = new List<SelectListItem>
    {
        new SelectListItem { Value = "Admin", Text = "Администратор" },
        new SelectListItem { Value = "Receptionist", Text = "Регистратор" },
        new SelectListItem { Value = "ReferringDoctor", Text = "Врач-терапевт (назначающий)" },
        new SelectListItem { Value = "ExecutingDoctor", Text = "Врач-специалист (исполняющий)" }
    };
            ViewBag.SystemRoles = new SelectList(roles, "Value", "Text", employee.SystemRole);

            return View(employee);
        }

        // GET: Employees/Edit/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.ProcedureRoom)
                .Include(e => e.IdentityUser)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null) return NotFound();

            var positions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Главный врач", Text = "Главный врач" },
                new SelectListItem { Value = "Врач терапевт", Text = "Врач терапевт" },
                new SelectListItem { Value = "Врач физиотерапевт", Text = "Врач физиотерапевт" },
                new SelectListItem { Value = "Врач реабилитолог", Text = "Врач реабилитолог" },
                new SelectListItem { Value = "Массажист", Text = "Массажист" },
                new SelectListItem { Value = "Инструктор ЛФК", Text = "Инструктор ЛФК" },
                new SelectListItem { Value = "Регистратор", Text = "Регистратор" },
                new SelectListItem { Value = "Администратор", Text = "Администратор" }
            };
            ViewBag.Positions = new SelectList(positions, "Value", "Text");

            var specializations = new List<SelectListItem>
            {
                new SelectListItem { Value = "Терапия", Text = "Терапия" },
                new SelectListItem { Value = "Физиотерапия", Text = "Физиотерапия" },
                new SelectListItem { Value = "Массаж", Text = "Массаж" },
                new SelectListItem { Value = "ЛФК", Text = "ЛФК" },
                new SelectListItem { Value = "Реабилитология", Text = "Реабилитология" }
            };
            ViewBag.Specializations = new SelectList(specializations, "Value", "Text", employee.Specialization);

            var rooms = await _context.ProcedureRooms
                .Select(r => new
                {
                    r.Id,
                    DisplayName = $"№{r.RoomNumber} - {r.Name}"
                })
                .ToListAsync();
            ViewBag.ProcedureRoomId = new SelectList(rooms, "Id", "DisplayName", employee.ProcedureRoomId);

            var roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Admin", Text = "Администратор" },
                new SelectListItem { Value = "Receptionist", Text = "Регистратор" },
                new SelectListItem { Value = "ReferringDoctor", Text = "Врач-терапевт (назначающий)" },
                new SelectListItem { Value = "ExecutingDoctor", Text = "Врач-специалист (исполняющий)" }
            };
            ViewBag.SystemRoles = new SelectList(roles, "Value", "Text", employee.SystemRole);

            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Position,Specialization,Phone,Email,ProcedureRoomId,HireDate,Salary,IsActive,HasSystemAccess,SystemRole")] Employee employee)
        {
            if (id != employee.Id) return NotFound();

            if (Request.Form.ContainsKey("Salary"))
            {
                var salaryStr = Request.Form["Salary"].ToString().Trim();
                if (string.IsNullOrEmpty(salaryStr))
                {
                    ModelState.AddModelError("Salary", "Поле Зарплата обязательно для заполнения");
                }
                else
                {
                    salaryStr = System.Text.RegularExpressions.Regex.Replace(salaryStr, @"[^\d,.]", "").Replace(",", ".");
                    ModelState.Remove("Salary");
                    if (decimal.TryParse(salaryStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal salaryValue))
                    {
                        employee.Salary = salaryValue;
                    }
                    else
                    {
                        ModelState.AddModelError("Salary", "Введите корректное число (например: 50000 или 50000.50)");
                    }
                }
            }

            // ПРОВЕРКА: если включён доступ в систему, то Email и SystemRole обязательны
            if (employee.HasSystemAccess)
            {
                if (string.IsNullOrWhiteSpace(employee.Email))
                {
                    ModelState.AddModelError("Email", "Для доступа в систему необходимо указать Email");
                }
                if (string.IsNullOrWhiteSpace(employee.SystemRole))
                {
                    ModelState.AddModelError("SystemRole", "Для доступа в систему необходимо выбрать роль");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingEmployee = await _context.Employees
                        .Include(e => e.IdentityUser)
                        .FirstOrDefaultAsync(e => e.Id == id);

                    if (existingEmployee == null) return NotFound();

                    existingEmployee.FullName = employee.FullName;
                    existingEmployee.Position = employee.Position;
                    existingEmployee.Specialization = employee.Specialization;
                    existingEmployee.Phone = employee.Phone;
                    existingEmployee.Email = employee.Email;
                    existingEmployee.ProcedureRoomId = employee.ProcedureRoomId;
                    existingEmployee.HireDate = employee.HireDate;
                    existingEmployee.Salary = employee.Salary;
                    existingEmployee.IsActive = employee.IsActive;
                    existingEmployee.HasSystemAccess = employee.HasSystemAccess;
                    existingEmployee.SystemRole = employee.SystemRole;

                    _context.Update(existingEmployee);
                    await _context.SaveChangesAsync();

                    await UpdateUserAccount(existingEmployee);

                    TempData["SuccessMessage"] = $"Сотрудник {employee.FullName} обновлён.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // Перезагружаем ViewBag при ошибке
            var positions = new List<SelectListItem>
    {
        new SelectListItem { Value = "Главный врач", Text = "Главный врач" },
        new SelectListItem { Value = "Врач терапевт", Text = "Врач терапевт" },
        new SelectListItem { Value = "Врач физиотерапевт", Text = "Врач физиотерапевт" },
        new SelectListItem { Value = "Врач реабилитолог", Text = "Врач реабилитолог" },
        new SelectListItem { Value = "Массажист", Text = "Массажист" },
        new SelectListItem { Value = "Инструктор ЛФК", Text = "Инструктор ЛФК" },
        new SelectListItem { Value = "Регистратор", Text = "Регистратор" },
        new SelectListItem { Value = "Администратор", Text = "Администратор" }
    };
            ViewBag.Positions = new SelectList(positions, "Value", "Text");

            var specializations = new List<SelectListItem>
    {
        new SelectListItem { Value = "Терапия", Text = "Терапия" },
        new SelectListItem { Value = "Физиотерапия", Text = "Физиотерапия" },
        new SelectListItem { Value = "Массаж", Text = "Массаж" },
        new SelectListItem { Value = "ЛФК", Text = "ЛФК" },
        new SelectListItem { Value = "Реабилитология", Text = "Реабилитология" }
    };
            ViewBag.Specializations = new SelectList(specializations, "Value", "Text");

            var rooms = await _context.ProcedureRooms
                .Select(r => new
                {
                    r.Id,
                    DisplayName = $"№{r.RoomNumber} - {r.Name}"
                })
                .ToListAsync();
            ViewBag.ProcedureRoomId = new SelectList(rooms, "Id", "DisplayName", employee.ProcedureRoomId);

            var roles = new List<SelectListItem>
    {
        new SelectListItem { Value = "Admin", Text = "Администратор" },
        new SelectListItem { Value = "Receptionist", Text = "Регистратор" },
        new SelectListItem { Value = "ReferringDoctor", Text = "Врач-терапевт (назначающий)" },
        new SelectListItem { Value = "ExecutingDoctor", Text = "Врач-специалист (исполняющий)" }
    };
            ViewBag.SystemRoles = new SelectList(roles, "Value", "Text", employee.SystemRole);

            return View(employee);
        }

        // POST: Employees/ResetPassword/5
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<JsonResult> ResetPassword(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.IdentityUser)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null || employee.IdentityUser == null)
            {
                return Json(new { success = false, message = "Сотрудник не найден или не имеет доступа к системе" });
            }

            var newPassword = GenerateTempPassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(employee.IdentityUser);
            var result = await _userManager.ResetPasswordAsync(employee.IdentityUser, token, newPassword);

            if (result.Succeeded)
            {
                return Json(new { success = true, password = newPassword });
            }

            return Json(new { success = false, message = "Ошибка сброса пароля" });
        }

        // GET: Employees/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.ProcedureRoom)
                .Include(e => e.IdentityUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.IdentityUser)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee != null)
            {
                if (employee.IdentityUser != null)
                {
                    var user = await _userManager.FindByIdAsync(employee.IdentityUserId);
                    if (user != null)
                    {
                        user.IsBlocked = true;
                        await _userManager.UpdateAsync(user);
                    }
                }
                _context.Employees.Remove(employee);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Schedule
        [Authorize(Policy = "AdminOrMedicalStaff")]
        public async Task<IActionResult> Schedule(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");

            var currentUserEmail = User.Identity.Name;
            var currentUser = await _userManager.FindByEmailAsync(currentUserEmail);

            List<Employee> employees;

            if (User.IsInRole("Admin") || User.IsInRole("ReferringDoctor"))
            {
                employees = await _context.Employees
                    .Include(e => e.ProcedureRoom)
                    .Where(e => e.IsActive == true && e.HireDate.Date <= selectedDate.Date)
                    .OrderBy(e => e.FullName)
                    .ToListAsync();
            }
            else
            {
                var currentEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.IdentityUserId == currentUser.Id);

                if (currentEmployee != null)
                {
                    employees = new List<Employee> { currentEmployee };
                }
                else
                {
                    employees = new List<Employee>();
                }
            }

            var assignments = await _context.ProcedureAssignments
                .Include(a => a.Client)
                .Include(a => a.Procedure)
                .Include(a => a.Employee)
                .Where(a => a.ProcedureDate.Date == selectedDate.Date && a.Status != "Отменена")
                .ToListAsync();

            ViewBag.Employees = employees;
            ViewBag.Assignments = assignments;

            return View();
        }

        // GET: Employees/GetEmployeeSchedule
        [HttpGet]
        public async Task<JsonResult> GetEmployeeSchedule(int employeeId, DateTime date)
        {
            var assignments = await _context.ProcedureAssignments
                .Include(a => a.Client)
                .Include(a => a.Procedure)
                .Where(a => a.EmployeeId == employeeId && a.ProcedureDate.Date == date.Date)
                .ToListAsync();

            var timeSlots = new List<object>();

            for (int hour = 8; hour <= 15; hour++)
            {
                for (int minute = 0; minute < 60; minute += 30)
                {
                    if (hour == 15 && minute == 30)
                    {
                        var slotStart = new TimeSpan(15, 30, 0);
                        var slotEnd = new TimeSpan(16, 0, 0);
                        var bookedAssignment = assignments.FirstOrDefault(a => a.StartTime < slotEnd && a.EndTime > slotStart);

                        bool slotIsAvailable = bookedAssignment == null || bookedAssignment.Status == "Отменена";
                        string slotStatus = bookedAssignment?.Status ?? "";

                        timeSlots.Add(new
                        {
                            startTime = slotStart.ToString(@"hh\:mm"),
                            endTime = slotEnd.ToString(@"hh\:mm"),
                            isAvailable = slotIsAvailable,
                            isLunch = false,
                            procedureName = bookedAssignment?.Procedure?.Name ?? "",
                            clientName = bookedAssignment?.Client?.FullName ?? "",
                            assignmentId = bookedAssignment?.Id ?? 0,
                            duration = bookedAssignment?.Procedure?.DurationMinutes ?? 0,
                            status = slotStatus
                        });
                        continue;
                    }

                    if (hour == 12 && minute == 0)
                    {
                        timeSlots.Add(new
                        {
                            startTime = "12:00",
                            endTime = "13:00",
                            isAvailable = false,
                            isLunch = true,
                            procedureName = "Обеденный перерыв",
                            clientName = "",
                            assignmentId = 0,
                            duration = 60,
                            status = ""
                        });
                        continue;
                    }

                    var currentStart = new TimeSpan(hour, minute, 0);
                    var currentEnd = currentStart.Add(TimeSpan.FromMinutes(30));
                    var currentBooking = assignments.FirstOrDefault(a => a.StartTime < currentEnd && a.EndTime > currentStart && a.Status != "Отменена");

                    bool currentIsAvailable = currentBooking == null;
                    string currentStatus = currentBooking?.Status ?? "";

                    timeSlots.Add(new
                    {
                        startTime = currentStart.ToString(@"hh\:mm"),
                        endTime = currentEnd.ToString(@"hh\:mm"),
                        isAvailable = currentIsAvailable,
                        isLunch = false,
                        procedureName = currentBooking?.Procedure?.Name ?? "",
                        clientName = currentBooking?.Client?.FullName ?? "",
                        assignmentId = currentBooking?.Id ?? 0,
                        duration = currentBooking?.Procedure?.DurationMinutes ?? 0,
                        status = currentStatus
                    });
                }
            }

            return Json(timeSlots);
        }

        // GET: Employees/GetByRoom
        [HttpGet]
        public async Task<JsonResult> GetByRoom(int roomId)
        {
            var employee = await _context.Employees
                .Where(e => e.ProcedureRoomId == roomId && e.IsActive)
                .Select(e => new { e.Id, e.FullName, e.Position })
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                return Json(new { success = false, message = "К кабинету не привязан ни один сотрудник" });
            }

            return Json(new { success = true, employeeId = employee.Id, employeeName = employee.FullName });
        }

        // GET: Employees/GetAvailable
        [HttpGet]
        public async Task<JsonResult> GetAvailable(DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            var busyEmployeeIds = await _context.ProcedureAssignments
                .Where(a => a.ProcedureDate.Date == date.Date &&
                            a.Status != "Отменена" &&
                            a.StartTime < endTime &&
                            a.EndTime > startTime)
                .Select(a => a.EmployeeId)
                .ToListAsync();

            var availableEmployees = await _context.Employees
                .Where(e => e.IsActive && !busyEmployeeIds.Contains(e.Id))
                .Select(e => new { e.Id, e.FullName, e.Position, e.Specialization })
                .ToListAsync();

            return Json(availableEmployees);
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }

        private async Task<string> CreateUserAccount(Employee employee)
        {
            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

            var existingUser = await userManager.FindByEmailAsync(employee.Email);
            if (existingUser != null)
            {
                employee.IdentityUserId = existingUser.Id;
                employee.IdentityUser = existingUser;
                await _context.SaveChangesAsync();
                return null;
            }

            var tempPassword = GenerateTempPassword();

            var user = new ApplicationUser
            {
                UserName = employee.Email,
                Email = employee.Email,
                EmailConfirmed = true,
                FullName = employee.FullName,
                EmployeeId = employee.Id,
                IsBlocked = false
            };

            var result = await userManager.CreateAsync(user, tempPassword);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(employee.SystemRole))
                {
                    await userManager.AddToRoleAsync(user, employee.SystemRole);
                }

                employee.IdentityUserId = user.Id;
                employee.IdentityUser = user;
                await _context.SaveChangesAsync();

                return tempPassword;
            }

            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Ошибка создания пользователя: {error.Description}");
            }

            return null;
        }

        // POST: Employees/ClearPasswordTempData
        [HttpPost]
        public IActionResult ClearPasswordTempData()
        {
            TempData.Remove("ShowPasswordModal");
            TempData.Remove("NewPassword");
            TempData.Remove("NewEmployeeName");
            TempData.Remove("NewEmployeeEmail");
            TempData.Remove("NewEmployeeRole");
            return Ok();
        }

        private async Task UpdateUserAccount(Employee employee)
        {
            if (employee.HasSystemAccess && !string.IsNullOrEmpty(employee.Email) && !string.IsNullOrEmpty(employee.SystemRole))
            {
                var user = await _userManager.FindByEmailAsync(employee.Email);

                if (user == null)
                {
                    await CreateUserAccount(employee);
                }
                else
                {
                    user.FullName = employee.FullName;
                    user.EmployeeId = employee.Id;
                    await _userManager.UpdateAsync(user);

                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, employee.SystemRole);

                    if (!employee.IsActive)
                    {
                        user.IsBlocked = true;
                        await _userManager.UpdateAsync(user);
                    }
                    else
                    {
                        user.IsBlocked = false;
                        await _userManager.UpdateAsync(user);
                    }

                    employee.IdentityUserId = user.Id;
                    await _context.SaveChangesAsync();
                }
            }
            else if (!employee.HasSystemAccess && employee.IdentityUserId != null)
            {
                var user = await _userManager.FindByIdAsync(employee.IdentityUserId);
                if (user != null)
                {
                    user.IsBlocked = true;
                    await _userManager.UpdateAsync(user);
                }
            }
        }

        private string GenerateTempPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var password = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return password + "1!";
        }
    }
}