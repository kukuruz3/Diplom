using System.ComponentModel.DataAnnotations;

namespace SanatoriumIS.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string? UserName { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty;

        public string? EntityName { get; set; }

        public string? EntityId { get; set; }

        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}