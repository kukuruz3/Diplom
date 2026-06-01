using System.ComponentModel.DataAnnotations;

namespace SanatoriumIS.Models
{
    public class RoomPrice
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Укажите вместимость")]
        [Range(1, 2, ErrorMessage = "Вместимость может быть 1 или 2")]
        [Display(Name = "Количество мест")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Укажите категорию")]
        [Display(Name = "Категория номера")]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите цену за ночь")]
        [Range(300, 10000, ErrorMessage = "Цена должна быть от 300 до 10 000 рублей")]
        [DataType(DataType.Currency)]
        [Display(Name = "Цена за ночь (₽)")]
        public decimal PricePerNight { get; set; }

        [Display(Name = "Дата начала действия")]
        [DataType(DataType.Date)]
        public DateTime ValidFrom { get; set; } = DateTime.Today;

        [Display(Name = "Описание")]
        [StringLength(200)]
        public string? Description { get; set; }
    }
}