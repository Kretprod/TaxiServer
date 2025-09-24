using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using server.Data;

public class RideHistoryCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RideHistoryCleanupService> _logger; // поле логгера

    public RideHistoryCleanupService(IServiceProvider serviceProvider, ILogger<RideHistoryCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Запускаем цикл, который раз в 24 часа выполняет удаление
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var thresholdDate = DateTime.UtcNow.AddDays(-14);

                    var oldRecords = await db.RideHistories
                        .Where(r => r.CompletedAt < thresholdDate)
                        .ToListAsync(stoppingToken);

                    if (oldRecords.Any())
                    {
                        db.RideHistories.RemoveRange(oldRecords);
                        await db.SaveChangesAsync(stoppingToken);
                        // Логируем информацию об удалении
                        _logger.LogInformation("Удалено {Count} записей истории поездок старше 14 дней.", oldRecords.Count);
                    }
                    else
                    {
                        // Логируем, что записей для удаления не найдено
                        _logger.LogInformation("Записей для удаления не найдено.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке RideHistory");
            }

            // Ждём 24 часа
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
