using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ForestFireDetection.Models
{
    public class SensorData
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Sensor")]
        [JsonPropertyName("Id")]
        public Guid SensorId { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("tmp")]
        public float Temperature { get; set; }

        [JsonPropertyName("hmd")]
        public float Humidity { get; set; }

        [JsonPropertyName("smk")]
        public float Smoke { get; set; }
        public DateTime Timestamp { get; set; }

        public Sensor Sensor { get; set; }
    }
}
