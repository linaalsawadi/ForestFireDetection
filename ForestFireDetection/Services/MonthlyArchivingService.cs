using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Models;
using Microsoft.Extensions.Logging;

namespace ForestFireDetection.Services
{
    public class MonthlyArchivingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonthlyArchivingService> _logger;
        private const int BatchSize = 1000;

        public MonthlyArchivingService(
            IServiceProvider serviceProvider,
            ILogger<MonthlyArchivingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ArchiveOldDataAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Archiving error");
                }

                var now = DateTime.UtcNow;
                var nextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                var delay = nextMonth - now;
                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task ArchiveOldDataAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ForestFireDetectionDbContext>();

            var cutoffDate = DateTime.UtcNow.AddMonths(-1);
            int totalArchived = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var batch = await context.SensorData
                    .Where(d => d.Timestamp < cutoffDate)
                    .OrderBy(d => d.Timestamp)
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0) break;

                var archiveList = batch.Select(d => new SensorDataArchive
                {
                    SensorId = d.SensorId,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    Temperature = d.Temperature,
                    Humidity = d.Humidity,
                    Smoke = d.Smoke,
                    FireScore = d.FireScore,
                    Timestamp = d.Timestamp
                }).ToList();

                context.SensorDataArchive.AddRange(archiveList);
                context.SensorData.RemoveRange(batch);
                await context.SaveChangesAsync(cancellationToken);

                totalArchived += batch.Count;
            }

            if (totalArchived > 0)
                _logger.LogInformation("Archived {Count} records older than {Date:yyyy-MM-dd}",
                    totalArchived, cutoffDate);
        }
    }
}
