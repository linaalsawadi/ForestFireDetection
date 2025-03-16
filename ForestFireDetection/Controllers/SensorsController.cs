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

        public IActionResult Edit(Guid id)
        {
            var sensor = context.Sensors.Find(id);
            if (sensor == null)
            {
                return RedirectToAction("Index", "Sensors");
            }

            SensorDto sensorDto = new SensorDto()
            {
                SensorLocation = sensor.SensorLocation,
                SensorState = sensor.SensorState,
                SensorPositioningDate = sensor.SensorPositioningDate,
                SensorDangerSituation = sensor.SensorDangerSituation,
            };

            ViewData["SensorId"] = sensor.SensorId;
            ViewData["SensorLocation"] = sensor.SensorLocation;
            ViewData["SensorState"] = sensor.SensorState;
            ViewData["SensorPositioningDate"] = sensor.SensorPositioningDate;


            return View(sensorDto);
        }

        [HttpPost]
        public IActionResult Edit(Guid id, SensorDto sensorDto)
        {
            var sensor = context.Sensors.Find(id);
            if (sensor == null)
            {
                return RedirectToAction("Index", "Sensors");
            }

            if (!ModelState.IsValid)
            {
                ViewData["SensorId"] = sensor.SensorId;
                ViewData["SensorLocation"] = sensor.SensorLocation;
                ViewData["SensorState"] = sensor.SensorState;
                ViewData["SensorPositioningDate"] = sensor.SensorPositioningDate.ToString("MM/dd/yyyy");

                return View(sensorDto);
            }

            sensor.SensorLocation = sensorDto.SensorLocation;
            sensor.SensorState = sensorDto.SensorState;
            sensor.SensorPositioningDate = sensorDto.SensorPositioningDate;
            sensor.SensorDangerSituation = sensorDto.SensorDangerSituation;

            context.SaveChanges();
            return RedirectToAction("Index", "Sensors");
        }
    }
}
