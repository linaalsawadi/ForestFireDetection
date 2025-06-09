using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Models;

public class HourlyAggregationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public HourlyAggregationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunHourlyAggregation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aggregation Error: {ex.Message}");
            }

            // نحسب الوقت حتى رأس الساعة القادمة (الساعة الدقيقة التالية)
            var now = DateTime.UtcNow;
            var nextHour = now.AddHours(1).Date.AddHours(now.AddHours(1).Hour);
            var delay = nextHour - now;

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task RunHourlyAggregation()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ForestFireDetectionDbContext>();

            var now = DateTime.UtcNow;
            var previousHour = now.AddHours(-1);
            var dateOnly = previousHour.Date;
            var hourOnly = previousHour.Hour;

            // منع التكرار إن كانت الساعة قد تم تلخيصها
            var exists = await context.SensorHourlySummary
                .AnyAsync(s => s.Date == dateOnly && s.Hour == hourOnly);

            if (exists) return;

            var summaries = await context.SensorData
                .Where(d => d.Timestamp >= previousHour && d.Timestamp < now)
                .GroupBy(d => d.SensorId)
                .Select(g => new SensorHourlySummary
                {
                    SensorId = g.Key,
                    Date = dateOnly,
                    Hour = hourOnly,
                    AvgTemperature = g.Average(x => x.Temperature),
                    AvgHumidity = g.Average(x => x.Humidity),
                    AvgSmoke = g.Average(x => x.Smoke),
                    AvgFireScore = g.Average(x => x.FireScore),
                    Latitude = g.Average(x => x.Latitude),
                    Longitude = g.Average(x => x.Longitude)
                })
                .ToListAsync();

            if (summaries.Any())
            {
                context.SensorHourlySummary.AddRange(summaries);
                await context.SaveChangesAsync();
                Console.WriteLine($"Hourly summary inserted for {dateOnly} at hour {hourOnly}.");
            }
        }
    }
}
