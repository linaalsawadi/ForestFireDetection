using System;
using System.ComponentModel.DataAnnotations;

namespace ForestFireDetection.Models
{
    public class SensorHourlySummary
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SensorId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int Hour { get; set; }

        public double AvgTemperature { get; set; }

        public double AvgHumidity { get; set; }

        public double AvgSmoke { get; set; }

        public double AvgFireScore { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
