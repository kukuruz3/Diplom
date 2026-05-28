using System.ComponentModel.DataAnnotations;

namespace SanatoriumIS.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите ФИО сотрудника")]
        [Display(Name = "ФИО")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "ФИО должно содержать от 3 до 100 символов")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Выберите должность")]
        [Display(Name = "Должность")]
        public string? Position { get; set; }

        [Display(Name = "Телефон")]
        [RegularExpression(@"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$", ErrorMessage = "Неверный формат телефона. Пример: +7 (123) 456-78-90")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Дата приема на работу")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }

        [Display(Name = "Зарплата")]
        [Range(0, 500000, ErrorMessage = "Зарплата должна быть от 0 до 500 000 рублей")]
        public decimal Salary { get; set; }

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Специализация")]
        public string? Specialization { get; set; }

        [Display(Name = "Основной кабинет")]
        public int? ProcedureRoomId { get; set; }

        [Display(Name = "Основной кабинет")]
        public ProcedureRoom? ProcedureRoom { get; set; }

        public virtual ICollection<ProcedureAssignment>? ProcedureAssignments { get; set; }

        // Связь с учётной записью пользователя
        public string? IdentityUserId { get; set; }
        public virtual ApplicationUser? IdentityUser { get; set; }

        // Есть ли доступ в систему
        [Display(Name = "Доступ в систему")]
        public bool HasSystemAccess { get; set; } = false;

        // НОВОЕ ПОЛЕ: Роль в системе
        [Display(Name = "Роль в системе")]
        public string? SystemRole { get; set; }
    }
}