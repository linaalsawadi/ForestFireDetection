using System.ComponentModel.DataAnnotations;

namespace ForestFireDetection.Models
{
    public class Sensor
    {
        [Key]
        [MaxLength(450)]
        public string SensorId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string SensorState { get; set; } = "green";

        public DateTime SensorPositioningDate { get; set; }

        public bool SensorDangerSituation { get; set; }

        public ICollection<SensorData> DataHistory { get; set; } = new List<SensorData>();

        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}
