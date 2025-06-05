namespace ForestFireDetection.Models.ViewModels
{
    public class SensorWithLastDataViewModel
    {
        public Sensor Sensor { get; set; }
        public SensorData LastData { get; set; }
    }
}