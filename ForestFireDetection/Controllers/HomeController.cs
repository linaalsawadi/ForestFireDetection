using ForestFireDetection.Data;
using ForestFireDetection.Models;
using ForestFireDetection.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ForestFireDetection.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ForestFireDetectionDbContext _context;

        public HomeController(ILogger<HomeController> logger, ForestFireDetectionDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var start = now.Date.AddHours(now.Hour - 23);

            var alerts = await _context.Alerts
                .Where(a => a.Timestamp >= start)
                .ToListAsync();

            var hourlyCounts = alerts
                .GroupBy(a => new { a.Timestamp.Year, a.Timestamp.Month, a.Timestamp.Day, a.Timestamp.Hour })
                .Select(g => new
                {
                    Hour = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0),
                    Count = g.Count()
                })
                .OrderBy(x => x.Hour)
                .ToList();

            var labels = Enumerable.Range(0, 24).Select(i => start.AddHours(i)).ToList();
            var data = labels.Select(label => hourlyCounts.FirstOrDefault(x => x.Hour == label)?.Count ?? 0).ToList();

            ViewBag.ChartLabels = labels.Select(l => l.ToString("HH:mm")).ToList();
            ViewBag.ChartData = data;
            ViewBag.ActiveAlerts = await _context.Alerts.CountAsync(a => a.Status == "NotReviewed");
            ViewBag.OnlineSensors = await _context.Sensors.CountAsync(s => s.SensorState != "offline");
            ViewBag.OfflineSensors = await _context.Sensors.CountAsync(s => s.SensorState == "offline");
            ViewBag.MaxFireScore = await _context.SensorData.AnyAsync()
                ? await _context.SensorData.MaxAsync(d => d.FireScore) : 0;
            ViewBag.RecentAlerts = await _context.Alerts
                .OrderByDescending(a => a.Timestamp).Take(5).ToListAsync();

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}