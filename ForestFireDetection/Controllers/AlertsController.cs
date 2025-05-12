using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForestFireDetection.Data;
using ForestFireDetection.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ForestFireDetection.Hubs;

namespace ForestFireDetection.Controllers
{
    [Authorize]
    public class AlertsController : Controller
    {
        private readonly ForestFireDetectionDbContext _context;
        private readonly IHubContext<AlertHub> _alertHub;

        public AlertsController(ForestFireDetectionDbContext context, IHubContext<AlertHub> alertHub)
        {
            _context = context;
            _alertHub = alertHub;
        }

        public async Task<IActionResult> Index()
        {
            var alerts = await _context.Alerts
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            return View(alerts);
        }

        [HttpPost]
        public async Task<IActionResult> Acknowledge(Guid id)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null || alert.Status != "NotReviewed")
                return NotFound();

            alert.Status = "InReview";
            alert.ReviewedBy = User.Identity?.Name ?? "Unknown";
            alert.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var notReviewedCount = await _context.Alerts.CountAsync(a => a.Status == "NotReviewed");
            await _alertHub.Clients.All.SendAsync("UpdateAlertCount", notReviewedCount);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Resolve(Guid id, string resolutionNote)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null || alert.Status != "InReview")
                return NotFound();

            if (alert.ReviewedBy != User.Identity?.Name)
                return Forbid();

            alert.Status = "Resolved";
            alert.ResolutionNote = resolutionNote;

            await _context.SaveChangesAsync();

            var notReviewedCount = await _context.Alerts.CountAsync(a => a.Status == "NotReviewed");
            await _alertHub.Clients.All.SendAsync("UpdateAlertCount", notReviewedCount);

            return Ok();
        }
    }
}
