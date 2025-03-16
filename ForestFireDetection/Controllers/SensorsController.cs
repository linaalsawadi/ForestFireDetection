using ForestFireDetection.Data;
using Microsoft.AspNetCore.Mvc;

namespace ForestFireDetection.Controllers
{
    public class SensorsController : Controller
    {
        private readonly ForestFireDetectionDbContext context;
        public SensorsController(ForestFireDetectionDbContext context)
        {
            this.context = context;
        }
        public IActionResult Index()
        {
            var sensors = context.Sensors.ToList();
            return View(sensors);
        }
    }
}
