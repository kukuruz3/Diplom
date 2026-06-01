using Microsoft.AspNetCore.Identity;
using SanatoriumIS.Data;
using SanatoriumIS.Models;
using System.Security.Claims;

namespace SanatoriumIS.Middleware
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
        {
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Логируем только успешные POST/PUT/DELETE
            if (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "DELETE")
            {
                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    using var scope = scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
                    var userName = context.User.Identity?.Name ?? "Anonymous";

                    var path = context.Request.Path.ToString();
                    var method = context.Request.Method;

                    // Получаем ID сущности из URL
                    var entityId = path.Split('/').LastOrDefault(s => int.TryParse(s, out _));

                    var log = new AuditLog
                    {
                        UserId = userId,
                        UserName = userName,
                        Action = $"{method} {path}",
                        EntityName = path.Split('/')[^2],
                        EntityId = entityId,
                        Timestamp = DateTime.Now
                    };

                    await dbContext.AuditLogs.AddAsync(log);
                    await dbContext.SaveChangesAsync();
                }
            }

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}