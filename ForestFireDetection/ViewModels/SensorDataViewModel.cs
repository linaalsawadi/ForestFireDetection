namespace ForestFireDetection.ViewModels
{
    public class SensorDataViewModel
    {
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Smoke { get; set; }
        public double FireScore { get; set; }
    }

}
