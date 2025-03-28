﻿using System.ComponentModel.DataAnnotations;

namespace ForestFireDetection.Models
{
    public class Sensor
    {
        [Required]
        public Guid SensorId { get; set; }
        [Required]
        [MaxLength(100)]
        public string SensorLocation { get; set; }
        [Required]
        [MaxLength(6)]
        public string SensorState { get; set; }//Explain if the sensor is green or yellow or red or gri
        [Required]
        public DateTime SensorPositioningDate { get; set; }
        public bool SensorDangerSituation { get; set; } = false;//if there is a fire true if not false
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public int Smoke { get; set; }
    }
}
