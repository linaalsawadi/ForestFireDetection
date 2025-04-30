using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using ForestFireDetection.Data;
using ForestFireDetection.Models;
using ForestFireDetection.Hubs;

public class MqttService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<AlertHub> _alertHub;
    private readonly IHubContext<MapHub> _mapHub;
    private IMqttClient _mqttClient;

    public MqttService(
       IServiceScopeFactory scopeFactory,
        IHubContext<AlertHub> alertHub,
        IHubContext<MapHub> mapHub)
    {
        _scopeFactory = scopeFactory;
        _alertHub = alertHub;
        _mapHub = mapHub;
    }

    public async Task StartAsync()
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("39602747e3bd405698f91bd71c84c53e.s1.eu.hivemq.cloud", 8883)
            .WithCredentials("soureya", "Rouya99911108")
            .WithTls()
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += async (e) =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var _context = scope.ServiceProvider.GetRequiredService<ForestFireDetectionDbContext>();
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                var data = JsonSerializer.Deserialize<SensorData>(payload);

                if (data == null) return;

                data.Id = Guid.NewGuid();
                data.Timestamp = DateTime.UtcNow;

                var sensor = await _context.Sensors.FindAsync(data.SensorId);
                if (sensor == null)
                {
                    sensor = new Sensor
                    {
                        SensorId = data.SensorId,
                        SensorPositioningDate = DateTime.UtcNow,
                        SensorState = "green",
                        SensorDangerSituation = false
                    };

                    _context.Sensors.Add(sensor);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("New sensor added: " + sensor.SensorId);
                }

                _context.SensorData.Add(data);

                string state;
                if (data.Temperature > 60 || data.Smoke > 80)
                    state = "red";
                else if (data.Temperature > 42 || data.Smoke > 50)
                    state = "yellow";
                else
                    state = "green";

                sensor.SensorState = state;
                sensor.SensorDangerSituation = (state != "green");

                await _context.SaveChangesAsync();

                await _mapHub.Clients.All.SendAsync("UpdateSensor", new
                {
                    sensorId = data.SensorId,
                    temperature = data.Temperature,
                    humidity = data.Humidity,
                    smoke = data.Smoke,
                    latitude = data.Latitude,
                    longitude = data.Longitude,
                    timestamp = data.Timestamp,
                    sensorState = state
                });

                if (state != "green")
                {
                    var alert = new Alert
                    {
                        Id = Guid.NewGuid(),
                        SensorId = data.SensorId,
                        Temperature = data.Temperature,
                        Smoke = data.Smoke,
                        Humidity = data.Humidity,
                        Timestamp = DateTime.UtcNow,
                        Latitude = data.Latitude,
                        Longitude = data.Longitude,
                        Status = "NotReviewed"
                    };

                    _context.Alerts.Add(alert);
                    await _context.SaveChangesAsync();

                    await _alertHub.Clients.All.SendAsync("NewAlert", new
                    {
                        alert.Id,
                        alert.SensorId,
                        alert.Temperature,
                        alert.Smoke,
                        alert.Humidity,
                        alert.Timestamp,
                        alert.Latitude,
                        alert.Longitude,
                        Status = alert.Status
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in MQTT handler:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        };

        try
        {
            await _mqttClient.ConnectAsync(options);
            Console.WriteLine("Connected to HiveMQ broker.");

            await _mqttClient.SubscribeAsync("forest/sensors");
            Console.WriteLine("Subscribed to topic: forest/sensors");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT Connection Error: {ex.Message}");
        }
    }
}
