using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanatoriumIS.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите ФИО")]
        [Display(Name = "ФИО")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "ФИО должно содержать от 3 до 100 символов")]
        public string? FullName { get; set; }

        // Оригинальный паспорт (не хранится в БД)
        [Display(Name = "Паспорт")]
        [RegularExpression(@"^\d{4}\s\d{6}$", ErrorMessage = "Неверный формат паспорта. Пример: 1234 567890")]
        [NotMapped] // Это поле не сохраняется в БД
        public string? PassportRaw { get; set; }

        // Хеш паспорта (хранится в БД)
        [Display(Name = "Паспорт")]
        public string? PassportHash { get; set; }

        [Display(Name = "Телефон")]
        [Required(ErrorMessage = "Введите номер телефона")]
        [RegularExpression(@"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$", ErrorMessage = "Неверный формат телефона. Пример: +7 (123) 456-78-90")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Введите дату рождения")]
        [Display(Name = "Дата рождения")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        // Проверка паспорта при вводе
        public bool VerifyPassport(string passport)
        {
            if (string.IsNullOrEmpty(PassportHash) || string.IsNullOrEmpty(passport))
                return false;
            return BCrypt.Net.BCrypt.Verify(passport, PassportHash);
        }
    }
}