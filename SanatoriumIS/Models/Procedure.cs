using System.ComponentModel.DataAnnotations;

namespace SanatoriumIS.Models
{
    public class Procedure
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название процедуры")]
        [Display(Name = "Название процедуры")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Название должно содержать от 3 до 100 символов")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Введите стоимость")]
        [Display(Name = "Стоимость")]
        [Range(100, 10000, ErrorMessage = "Стоимость должна быть от 100 до 10 000 рублей")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Введите длительность")]
        [Display(Name = "Длительность (минуты)")]
        [Range(5, 480, ErrorMessage = "Длительность должна быть от 5 до 480 минут")]
        public int DurationMinutes { get; set; }

        [Display(Name = "Тип процедуры")]
        public string? ProcedureType { get; set; }

        [Display(Name = "Требуемый тип кабинета")]
        public string? RequiredRoomType { get; set; }
    }
}