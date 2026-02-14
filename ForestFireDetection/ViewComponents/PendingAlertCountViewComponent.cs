using ForestFireDetection.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForestFireDetection.ViewComponents
{
    public class PendingAlertCountViewComponent : ViewComponent
    {
        private readonly ForestFireDetectionDbContext _context;

        public PendingAlertCountViewComponent(ForestFireDetectionDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var count = await _context.Alerts.CountAsync(a => a.Status == "NotReviewed");
            return View(count);
        }
    }
}