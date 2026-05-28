using System.ComponentModel.DataAnnotations;

namespace SanatoriumIS.Models
{
    public class ProcedureRoom
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите номер кабинета")]
        public string? RoomNumber { get; set; }

        [Required(ErrorMessage = "Введите название кабинета")]
        public string? Name { get; set; }

        public string? RoomType { get; set; }

        public string? Description { get; set; }

        public virtual ICollection<Employee>? Employees { get; set; }
        public virtual ICollection<ProcedureAssignment>? ProcedureAssignments { get; set; }
    }
}