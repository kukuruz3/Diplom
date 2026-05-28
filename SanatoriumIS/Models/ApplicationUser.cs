using Microsoft.AspNetCore.Identity;

namespace SanatoriumIS.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Связь с сотрудником
        public int? EmployeeId { get; set; }
        public virtual Employee? Employee { get; set; }

        // Полное имя для отображения
        public string? FullName { get; set; }

        // Дата последнего входа
        public DateTime? LastLoginDate { get; set; }

        // Флаг блокировки (дополнительно к LockoutEnabled)
        public bool IsBlocked { get; set; } = false;
    }
}