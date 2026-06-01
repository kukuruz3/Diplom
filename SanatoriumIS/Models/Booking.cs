using SanatoriumIS.Data;
using System.ComponentModel.DataAnnotations;

namespace SanatoriumIS.Models
{
    public class Booking : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Выберите клиента")]
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        [Required(ErrorMessage = "Выберите номер")]
        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Required(ErrorMessage = "Укажите дату заезда")]
        [DataType(DataType.Date)]
        public DateTime CheckIn { get; set; }

        [Required(ErrorMessage = "Укажите дату выезда")]
        [DataType(DataType.Date)]
        public DateTime CheckOut { get; set; }

        // Второй клиент для двухместного номера
        [Display(Name = "Второй клиент")]
        public int? SecondClientId { get; set; }
        public Client? SecondClient { get; set; }

        // Дополнительные услуги
        public virtual ICollection<BookingService>? BookingServices { get; set; }

        // Дата фактического выселения
        [Display(Name = "Дата выселения")]
        [DataType(DataType.Date)]
        public DateTime? CheckedOutAt { get; set; }

        [Display(Name = "Цена за ночь на момент бронирования")]
        public decimal PricePerNightAtBooking { get; set; }

        [Display(Name = "Общая стоимость проживания")]
        public decimal TotalPrice { get; set; }

        // Флаг выселения
        [Display(Name = "Выселен")]
        public bool IsCheckedOut { get; set; } = false;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var context = (ApplicationDbContext)validationContext.GetService(typeof(ApplicationDbContext));

            // Проверка дат
            if (CheckOut <= CheckIn)
            {
                yield return new ValidationResult(
                    "Дата выезда должна быть позже даты заезда",
                    new[] { nameof(CheckOut) });
            }

            // Проверка минимального срока
            if ((CheckOut - CheckIn).TotalDays < 1)
            {
                yield return new ValidationResult(
                    "Минимальный срок бронирования - 1 сутки",
                    new[] { nameof(CheckOut) });
            }

            // Проверка максимального срока
            if ((CheckOut - CheckIn).TotalDays > 30)
            {
                yield return new ValidationResult(
                    "Максимальный срок бронирования - 30 суток",
                    new[] { nameof(CheckOut) });
            }

            // Проверка: клиенты не должны совпадать
            if (SecondClientId.HasValue && SecondClientId.Value == ClientId)
            {
                yield return new ValidationResult(
                    "Первый и второй клиент не могут совпадать",
                    new[] { nameof(SecondClientId) });
            }

            // Проверка пересечения бронирований для первого клиента
            if (context != null)
            {
                var clientConflict = context.Bookings
                    .Any(b => b.ClientId == ClientId &&
                              b.Id != Id &&
                              b.CheckIn < CheckOut &&
                              b.CheckOut > CheckIn);

                if (clientConflict)
                {
                    yield return new ValidationResult(
                        "У этого клиента уже есть бронирование на выбранные даты",
                        new[] { nameof(ClientId) });
                }
            }

            // Проверка пересечения бронирований для второго клиента
            if (context != null && SecondClientId.HasValue)
            {
                var secondClientConflict = context.Bookings
                    .Any(b => b.ClientId == SecondClientId.Value &&
                              b.Id != Id &&
                              b.CheckIn < CheckOut &&
                              b.CheckOut > CheckIn);

                if (secondClientConflict)
                {
                    yield return new ValidationResult(
                        "У второго клиента уже есть бронирование на выбранные даты",
                        new[] { nameof(SecondClientId) });
                }
            }
        }
    }
}