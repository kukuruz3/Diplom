using SanatoriumIS.Models;

namespace SanatoriumIS.ViewModels
{
    public class ProcedureReportViewModel
    {
        public DateTime SelectedDate { get; set; }
        public int? SelectedClientId { get; set; }
        public List<Client> Clients { get; set; } = new();
        public List<ProcedureReportItem> ReportItems { get; set; } = new();
    }

    public class ProcedureReportItem
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime ProcedureDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Duration => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public string DurationMinutes => $"{(EndTime - StartTime).TotalMinutes} мин";
        public string Status { get; set; } = "Запланирована"; // Добавлено поле Status
    }
}