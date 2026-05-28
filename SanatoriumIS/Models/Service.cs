using System.ComponentModel.DataAnnotations;

namespace SanatoriumIS.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название услуги")]
        [Display(Name = "Название услуги")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Название должно содержать от 3 до 100 символов")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Введите стоимость")]
        [Display(Name = "Стоимость")]
        [Range(100, 10000, ErrorMessage = "Стоимость должна быть от 100 до 10 000 рублей")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }
    }
}