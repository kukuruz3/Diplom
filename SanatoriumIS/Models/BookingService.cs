using System.ComponentModel.DataAnnotations;

namespace SanatoriumIS.Models
{
    public class BookingService
    {
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Required]
        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        [Display(Name = "Количество")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Цена на момент продажи")]
        public decimal PriceAtTime { get; set; }

        [Display(Name = "Дата добавления")]
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}