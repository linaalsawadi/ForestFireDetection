using System;
using System.ComponentModel.DataAnnotations;

namespace ForestFireDetection.Models
{
    public class SensorDataArchive
    {
        [Key]
        public int Id { get; set; }

        public string SensorId { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public float Temperature { get; set; }

        public float Humidity { get; set; }

        public float Smoke { get; set; }

        public double FireScore { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
