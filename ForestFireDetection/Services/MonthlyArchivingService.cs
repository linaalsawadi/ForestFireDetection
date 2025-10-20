using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Models;

public class MonthlyArchivingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public MonthlyArchivingService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ArchiveOldData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Archiving Error: {ex.Message}");
            }

            // ننتظر حتى بداية الشهر القادم
            var now = DateTime.UtcNow;
            var nextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            var delay = nextMonth - now;

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task ArchiveOldData()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ForestFireDetectionDbContext>();

            var cutoffDate = DateTime.UtcNow.AddMonths(-1);

            var oldData = await context.SensorData
                .Where(d => d.Timestamp < cutoffDate)
                .ToListAsync();

            if (!oldData.Any()) return;

            var archiveList = oldData.Select(d => new SensorDataArchive
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
            context.SensorData.RemoveRange(oldData);

            await context.SaveChangesAsync();

            Console.WriteLine($"Archived {archiveList.Count} records older than {cutoffDate:yyyy-MM-dd}.");
        }
    }
}
