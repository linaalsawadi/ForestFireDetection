using System.ComponentModel.DataAnnotations;

namespace ForestFireDetection.Models
{
    public class Sensor
    {
        [Key]
        public string SensorId { get; set; }

        public string SensorState { get; set; }
        public DateTime SensorPositioningDate { get; set; }
        public bool SensorDangerSituation { get; set; }

        public ICollection<SensorData> DataHistory { get; set; }
        public ICollection<Alert> Alerts { get; set; }
    }
}
