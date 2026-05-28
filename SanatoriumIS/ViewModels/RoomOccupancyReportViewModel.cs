using SanatoriumIS.Models;

namespace SanatoriumIS.ViewModels
{
    public class RoomOccupancyReportViewModel
    {
        public DateTime SelectedDate { get; set; }
        public bool ShowOnlyOccupied { get; set; }
        public List<RoomOccupancyItem> ReportItems { get; set; } = new();
    }

    public class RoomOccupancyItem
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomCategory { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsOccupied { get; set; }
        public string Status => IsOccupied ? "Занят" : "Свободен";
        public string StatusBadge => IsOccupied ? "danger" : "success";
        public List<CurrentGuest> CurrentGuests { get; set; } = new();
    }

    public class CurrentGuest
    {
        public string ClientName { get; set; } = string.Empty;
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
    }
}