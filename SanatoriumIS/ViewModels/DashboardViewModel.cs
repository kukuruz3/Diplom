using System;
using System.Collections.Generic;
using SanatoriumIS.Models;

namespace SanatoriumIS.ViewModels
{
    public class DashboardViewModel
    {
        public int ClientsCount { get; set; }
        public int RoomsCount { get; set; }
        public int BookingsCount { get; set; }
        public int ProceduresCount { get; set; }
        public int ServicesCount { get; set; }
        public int FreeRoomsCount { get; set; }

        // Списки для отображения
        public List<Booking> TodayBookings { get; set; } = new();
        public List<Booking> UpcomingBookings { get; set; } = new();
        public List<ProcedureAssignment> RecentProcedures { get; set; } = new();
    }
}