namespace ForestFireDetection.Models.DTOs
{
    public class SensorDto
    {
        public string SensorId { get; set; }
        public string SensorState { get; set; }
        public DateTime SensorPositioningDate { get; set; }
        public bool SensorDangerSituation { get; set; }
        public double? FireScore { get; set; } // ✅ آخر FireScore من SensorData
    }
}