namespace ForestFireDetection.Models.Enums
{
    public enum SensorState
    {
        Green,
        Yellow,
        Red,
        Offline
    }

    public static class SensorStateExtensions
    {
        public static string ToLowerString(this SensorState state) => state switch
        {
            SensorState.Green => "green",
            SensorState.Yellow => "yellow",
            SensorState.Red => "red",
            SensorState.Offline => "offline",
            _ => "green"
        };

        public static SensorState FromString(string state) => state?.ToLower() switch
        {
            "green" => SensorState.Green,
            "yellow" => SensorState.Yellow,
            "red" => SensorState.Red,
            "offline" => SensorState.Offline,
            _ => SensorState.Green
        };
    }
}               