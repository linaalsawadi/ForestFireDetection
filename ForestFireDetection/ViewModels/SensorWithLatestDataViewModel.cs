namespace ForestFireDetection.ViewModels
{
    public class SensorWithLatestDataViewModel
    {
        public string SensorId { get; set; }
        public string SensorState { get; set; }
        public DateTime SensorPositioningDate { get; set; }
        public bool SensorDangerSituation { get; set; }

        // آخر بيانات مستلمة
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Smoke { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
