using System.ComponentModel.DataAnnotations;

namespace ForestFireDetection.Models
{
    public class Sensor
    {
        [Key]
        public Guid SensorId { get; set; }

        public string SensorState { get; set; }
        public DateTime SensorPositioningDate { get; set; }
        public bool SensorDangerSituation { get; set; }

        public ICollection<SensorData> DataHistory { get; set; }
    }
}
