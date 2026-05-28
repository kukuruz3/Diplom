using Microsoft.AspNetCore.Identity;
using SanatoriumIS.Models;

namespace SanatoriumIS.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            // Создаём роли
            string[] roles = { "Admin", "Receptionist", "ReferringDoctor", "ExecutingDoctor" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Создаём администратора
            var adminEmail = "admin@sanatorium.ru";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Системный администратор",
                    IsBlocked = false
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Ошибка: {error.Description}");
                    }
                }
            }
        }
    }
}