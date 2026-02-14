namespace ForestFireDetection.Models.DTOs
{
    public class SensorDto
    {
        public string SensorId { get; set; } = string.Empty;
        public string SensorState { get; set; } = string.Empty;
        public DateTime SensorPositioningDate { get; set; }
        public bool SensorDangerSituation { get; set; }
        public double? FireScore { get; set; }
    }
}