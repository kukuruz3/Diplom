using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanatoriumIS.Models
{
    public enum RoomStatus
    {
        Available = 0,   // Свободен
        Occupied = 1,    // Занят
        Inactive = 2     // Неактивен (ремонт/закрыт)
    }

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

        [Display(Name = "Статус")]
        public RoomStatus Status { get; set; } = RoomStatus.Available;

        // Добавляем вспомогательное свойство для отображения
        [NotMapped]
        public string CapacityDisplay => Capacity == 1 ? "одноместный" : "двухместный";

        [NotMapped]
        public string StatusDisplay => Status switch
        {
            RoomStatus.Available => "Свободен",
            RoomStatus.Occupied => "Занят",
            RoomStatus.Inactive => "Неактивен",
            _ => "Неизвестно"
        };

        [NotMapped]
        public string StatusColor => Status switch
        {
            RoomStatus.Available => "#4caf50",
            RoomStatus.Occupied => "#f44336",
            RoomStatus.Inactive => "#9e9e9e",
            _ => "#cccccc"
        };

        public virtual ICollection<Booking>? Bookings { get; set; }
    }
}