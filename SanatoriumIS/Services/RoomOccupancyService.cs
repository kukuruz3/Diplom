using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Data;

namespace SanatoriumIS.Services
{
    public class RoomOccupancyService
    {
        private readonly IServiceProvider _serviceProvider;

        public RoomOccupancyService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task UpdateRoomsOccupancyStatus()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.Today;

            // Находим все активные бронирования на сегодня
            var activeBookings = await context.Bookings
                .Where(b => b.CheckIn.Date <= today && b.CheckOut.Date > today)
                .Select(b => b.RoomId)
                .Distinct()
                .ToListAsync();

            // Получаем все номера
            var allRooms = await context.Rooms.ToListAsync();

            // Обновляем статус
            foreach (var room in allRooms)
            {
                var shouldBeOccupied = activeBookings.Contains(room.Id);
                if (room.IsOccupied != shouldBeOccupied)
                {
                    room.IsOccupied = shouldBeOccupied;
                }
            }

            await context.SaveChangesAsync();
        }
    }
}