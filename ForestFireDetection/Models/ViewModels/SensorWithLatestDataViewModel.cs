namespace ForestFireDetection.Models.ViewModels
{
    public class SensorWithLatestDataViewModel
    {
        public string SensorId { get; set; } = string.Empty;
        public string SensorState { get; set; } = string.Empty;
        public DateTime SensorPositioningDate { get; set; }
        public bool SensorDangerSituation { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Smoke { get; set; }
        public DateTime Timestamp { get; set; }
    }
}