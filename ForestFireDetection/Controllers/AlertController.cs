using ForestFireDetection.Data;
using ForestFireDetection.Models;
using Microsoft.AspNetCore.Mvc;

namespace ForestFireDetection.Controllers
{
    public class AlertController : Controller
    {
        private readonly ForestFireDetectionDbContext _context;

        public AlertController(ForestFireDetectionDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult AcknowledgeAlert(int sensorId, string location, string description)
        {
            var alert = new Alert
            {
                //SensorId = sensorId,
                //AlertTime = DateTime.UtcNow,
                //Acknowledged = true,
                //Location = location,
                //Description = description
            };

            _context.Alerts.Add(alert);
            _context.SaveChanges();

            return Ok(new { message = "Alert acknowledged and saved successfully." });
        }
    }

}
