namespace SanatoriumIS.Services
{
    public class RoomOccupancyBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RoomOccupancyBackgroundService> _logger;

        public RoomOccupancyBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<RoomOccupancyBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Запускаем в 00:00 каждый день
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1);
                var delay = nextRun - now;

                _logger.LogInformation($"Следующее обновление статуса номеров в {nextRun}");

                await Task.Delay(delay, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<RoomOccupancyService>();
                await service.UpdateRoomsOccupancyStatus();

                _logger.LogInformation($"Статус номеров обновлён в {DateTime.Now}");
            }
        }
    }
}