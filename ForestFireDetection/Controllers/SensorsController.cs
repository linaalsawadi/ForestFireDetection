using ForestFireDetection.Data;
using ForestFireDetection.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace ForestFireDetection.Controllers
{
    public class SensorsController : Controller
    {
        private readonly ForestFireDetectionDbContext context;
        private readonly IWebHostEnvironment environment;

        public SensorsController(ForestFireDetectionDbContext context,IWebHostEnvironment environment)
        {
            this.context = context;
            this.environment = environment;
        }
        public IActionResult Index()
        {
            var sensors = context.Sensors.OrderByDescending(p=>p.SensorId).ToList();
            return View(sensors);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(SensorDto sensorDto)
        {
            if (!ModelState.IsValid)
            {
                return View(sensorDto);
            }

            Sensor sensor = new Sensor()
            {
                 
            SensorLocation = sensorDto.SensorLocation,
            SensorState = sensorDto.SensorState,
            SensorPositioningDate = sensorDto.SensorPositioningDate,
            SensorDangerSituation = sensorDto.SensorDangerSituation,
            };

            context.Sensors.Add(sensor);
            context.SaveChanges();
            return RedirectToAction("Index", "Sensors");
        }

        public IActionResult Edit()
        {
            var sensors = context.Sensors.OrderByDescending(p => p.SensorId).ToList();
            return View(sensors);
        }
    }
}
