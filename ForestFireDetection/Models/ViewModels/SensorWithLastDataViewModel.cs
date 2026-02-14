namespace ForestFireDetection.Models.ViewModels
{
    public class SensorWithLastDataViewModel
    {
        public Sensor Sensor { get; set; } = null!;
        public SensorData? LastData { get; set; }
    }
}