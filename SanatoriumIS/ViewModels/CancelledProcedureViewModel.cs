namespace SanatoriumIS.ViewModels
{
    public class CancelledProcedureViewModel
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime CancelledDate { get; set; }
        public string CancelReason { get; set; } = string.Empty;
        public string CancelledByName { get; set; } = string.Empty;
        public DateTime OriginalDate { get; set; }
        public TimeSpan OriginalTime { get; set; }
    }
}