using SanatoriumIS.Data;
using System.ComponentModel.DataAnnotations;

namespace SanatoriumIS.Models
{
    public class ProcedureAssignment : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Выберите клиента")]
        public int ClientId { get; set; }

        public Client? Client { get; set; }

        [Required(ErrorMessage = "Выберите процедуру")]
        public int ProcedureId { get; set; }

        public Procedure? Procedure { get; set; }

        [Required(ErrorMessage = "Выберите кабинет")]
        public int ProcedureRoomId { get; set; }

        public ProcedureRoom? ProcedureRoom { get; set; }

        [Required(ErrorMessage = "Выберите сотрудника")]
        public int EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        [Required(ErrorMessage = "Укажите дату процедуры")]
        [DataType(DataType.Date)]
        public DateTime ProcedureDate { get; set; }

        [Required(ErrorMessage = "Укажите время начала")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Укажите время окончания")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        // Статус процедуры
        [Display(Name = "Статус")]
        public string Status { get; set; } = "Запланирована";

        // Кто и когда отметил
        [Display(Name = "Дата выполнения")]
        public DateTime? CompletedAt { get; set; }

        [Display(Name = "Кто выполнил")]
        public string? CompletedBy { get; set; }

        [Display(Name = "Дата отмены")]
        public DateTime? CancelledAt { get; set; }

        [Display(Name = "Кто отменил")]
        public string? CancelledBy { get; set; }

        [Display(Name = "Причина отмены")]
        [StringLength(500, ErrorMessage = "Причина не более 500 символов")]
        public string? CancelReason { get; set; }

        [Display(Name = "Примечание")]
        [StringLength(500, ErrorMessage = "Примечание не более 500 символов")]
        public string? Note { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var context = (ApplicationDbContext)validationContext.GetService(typeof(ApplicationDbContext));

            // Обеденный перерыв с 12:00 до 13:00
            var lunchStart = new TimeSpan(12, 0, 0);
            var lunchEnd = new TimeSpan(13, 0, 0);

            // Процедура не должна начинаться во время обеда
            if (StartTime >= lunchStart && StartTime < lunchEnd)
            {
                yield return new ValidationResult(
                    "Нельзя начинать процедуру во время обеда (12:00-13:00)",
                    new[] { nameof(StartTime) });
            }

            // Процедура не должна заканчиваться во время обеда (если началась до обеда)
            if (StartTime < lunchStart && EndTime > lunchStart)
            {
                yield return new ValidationResult(
                    "Процедура не может заканчиваться во время обеда (12:00-13:00). Выберите более раннее время окончания или перенесите на время после обеда (13:00)",
                    new[] { nameof(EndTime) });
            }

            // Процедура не должна полностью пересекать обед
            if (StartTime < lunchEnd && EndTime > lunchStart)
            {
                yield return new ValidationResult(
                    "Процедура не может пересекаться с обеденным перерывом (12:00-13:00)",
                    new[] { nameof(StartTime), nameof(EndTime) });
            }

            // Проверка: время окончания позже времени начала
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    "Время окончания должно быть позже времени начала",
                    new[] { nameof(EndTime) });
            }

            // Проверка: минимальная длительность
            if ((EndTime - StartTime).TotalMinutes < 10)
            {
                yield return new ValidationResult(
                    "Минимальная длительность процедуры - 10 минут",
                    new[] { nameof(EndTime) });
            }

            // Проверка: рабочее время (8:00 - 16:00)
            if (StartTime < new TimeSpan(8, 0, 0))
            {
                yield return new ValidationResult(
                    "Процедуры могут начинаться не раньше 08:00",
                    new[] { nameof(StartTime) });
            }

            if (EndTime > new TimeSpan(16, 0, 0))
            {
                yield return new ValidationResult(
                    "Процедуры должны заканчиваться не позднее 16:00",
                    new[] { nameof(EndTime) });
            }

            // Проверка: дата не в прошлом
            if (ProcedureDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Дата процедуры не может быть раньше сегодняшнего дня",
                    new[] { nameof(ProcedureDate) });
            }

            // Проверка: пересечение процедур у клиента (только для НЕ отменённых)
            if (context != null)
            {
                var hasConflict = context.ProcedureAssignments
                    .Any(a => a.ClientId == ClientId &&
                        a.ProcedureDate.Date == ProcedureDate.Date &&
                        a.Id != Id &&
                        a.Status != "Отменена" &&
                        a.StartTime < EndTime &&
                        a.EndTime > StartTime);

                if (hasConflict)
                {
                    yield return new ValidationResult(
                        "У этого клиента уже есть процедура в выбранное время",
                        new[] { nameof(StartTime), nameof(EndTime) });
                }

                // Проверка бронирования
                var hasBooking = context.Bookings
                    .Any(b => b.ClientId == ClientId &&
                        b.CheckIn.Date <= ProcedureDate.Date &&
                        b.CheckOut.Date >= ProcedureDate.Date);

                if (!hasBooking)
                {
                    yield return new ValidationResult(
                        "У этого клиента нет активного бронирования на выбранную дату",
                        new[] { nameof(ClientId) });
                }
            }

            // Проверка даты трудоустройства сотрудника
            if (context != null && EmployeeId > 0)
            {
                var employee = context.Employees.Find(EmployeeId);
                if (employee != null && employee.HireDate.Date > ProcedureDate.Date)
                {
                    yield return new ValidationResult(
                        $"Сотрудник {employee.FullName} принят на работу {employee.HireDate.ToShortDateString()}",
                        new[] { nameof(ProcedureDate), nameof(EmployeeId) });
                }
            }
        }
    }
}