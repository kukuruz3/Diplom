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

            try
            {
                await _next(context);

                // Логируем только успешные POST/PUT/DELETE
                if (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "DELETE")
                {
                    if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 400)
                    {
                        try
                        {
                            using var scope = scopeFactory.CreateScope();
                            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
                            var userName = context.User.Identity?.Name ?? "Anonymous";

                            var path = context.Request.Path.ToString();
                            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                            var entityName = segments.Length >= 2 ? segments[^2] : (segments.Length == 1 ? segments[0] : "Unknown");
                            var entityId = path.Split('/').LastOrDefault(s => int.TryParse(s, out _));

                            var log = new AuditLog
                            {
                                UserId = userId,
                                UserName = userName,
                                Action = $"{context.Request.Method} {path}",
                                EntityName = entityName,
                                EntityId = entityId,
                                Timestamp = DateTime.Now
                            };

                            await dbContext.AuditLogs.AddAsync(log);
                            await dbContext.SaveChangesAsync();
                        }
                        catch
                        {
                            // Ошибка логирования не должна ломать основной запрос
                        }
                    }
                }
            }
            finally
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
        }
    }
}