using System;
using System.Collections.Generic;
using SanatoriumIS.Models;

namespace SanatoriumIS.ViewModels
{
    public class CalendarViewModel
    {
        public DateTime SelectedDate { get; set; }
        public int SelectedRoomId { get; set; }
        public List<ProcedureRoom> Rooms { get; set; } = new();
        public List<TimeSlot> TimeSlots { get; set; } = new();
        public List<BookedSlot> BookedSlots { get; set; } = new();
    }

    public class TimeSlot
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
        public string? ProcedureName { get; set; }
        public string? ClientName { get; set; }
        public int? AssignmentId { get; set; }
    }

    public class BookedSlot
    {
        public int Id { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
    }
}