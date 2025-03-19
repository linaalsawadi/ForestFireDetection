using ForestFireDetection.Data;
using ForestFireDetection.Models;
using Hospital_appointment_system.Data;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize(Roles = UserRoles.Admin)]
        public IActionResult Index()
        {
            var sensors = context.Sensors.OrderByDescending(p=>p.SensorId).ToList();
            return View(sensors);
        }

        [Authorize(Roles = UserRoles.Admin)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public IActionResult Create(Sensor sensor)
        {
            if (!ModelState.IsValid)
            {
                return View(sensor);
            }

            Sensor sensornew = new Sensor()
            {
            SensorLocation = sensor.SensorLocation,
            SensorState = sensor.SensorState,
            SensorPositioningDate = sensor.SensorPositioningDate,
            SensorDangerSituation = sensor.SensorDangerSituation,
            };

            context.Sensors.Add(sensornew);
            context.SaveChanges();
            return RedirectToAction("Index", "Sensors");
        }

        [Authorize(Roles = UserRoles.Admin)]
        public IActionResult Edit(Guid id)
        {
            var sensor = context.Sensors.Find(id);
            if (sensor == null)
            {
                return RedirectToAction("Index", "Sensors");
            }

            Sensor sensornew = new Sensor()
            {
                SensorLocation = sensor.SensorLocation,
                SensorPositioningDate = sensor.SensorPositioningDate,
                SensorDangerSituation = sensor.SensorDangerSituation,
            };

            ViewData["SensorId"] = sensor.SensorId;
            ViewData["SensorState"] = sensor.SensorState;


            return View(sensornew);
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        public IActionResult Edit(Guid id, Sensor sensornew)
        {
            var sensor = context.Sensors.Find(id);
            if (sensor == null)
            {
                return RedirectToAction("Index", "Sensors");
            }

            if (!ModelState.IsValid)
            {
                ViewData["SensorId"] = sensor.SensorId;
                ViewData["SensorState"] = sensor.SensorState;

                return View(sensornew);
            }

            sensor.SensorLocation = sensornew.SensorLocation;
            sensor.SensorPositioningDate = sensornew.SensorPositioningDate;
            sensor.SensorDangerSituation = sensornew.SensorDangerSituation;

            context.SaveChanges();
            return RedirectToAction("Index", "Sensors");
        }

        [Authorize(Roles = UserRoles.Admin)]
        public IActionResult Delete(Guid id)
        {
            var sensor = context.Sensors.Find(id);
            if (sensor == null)
            {
                return RedirectToAction("Index", "Sensors");
            }
            context.Sensors.Remove(sensor);
            context.SaveChanges(true);

            return RedirectToAction("Index","Sensors");
        }
    }
}
