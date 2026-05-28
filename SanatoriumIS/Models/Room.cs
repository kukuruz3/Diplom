using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanatoriumIS.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите номер комнаты")]
        [Display(Name = "Номер комнаты")]
        public string? Number { get; set; }

        [Required(ErrorMessage = "Введите вместимость")]
        [Display(Name = "Вместимость")]
        [Range(1, 2, ErrorMessage = "Вместимость может быть только 1 (одноместный) или 2 (двухместный)")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Выберите категорию")]
        [Display(Name = "Категория")]
        public string? Category { get; set; }

        [Display(Name = "Занят")]
        public bool IsOccupied { get; set; } = false;

        // Добавляем вспомогательное свойство для отображения
        [NotMapped]
        public string CapacityDisplay => Capacity == 1 ? "одноместный" : "двухместный";

        public virtual ICollection<Booking>? Bookings { get; set; }
    }
}