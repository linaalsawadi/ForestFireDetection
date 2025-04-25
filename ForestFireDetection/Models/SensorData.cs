using System.ComponentModel.DataAnnotations;

namespace ForestFireDetection.Models
{
    public class SensorData
    {
        public Guid Id { get; set; }
        public Guid SensorId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Smoke { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
