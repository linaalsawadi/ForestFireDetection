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
    private readonly IHubContext<ChartHub> _chartHub;
    private IMqttClient _mqttClient;

    public MqttService(
       IServiceScopeFactory scopeFactory,
        IHubContext<AlertHub> alertHub,
        IHubContext<MapHub> mapHub,
        IHubContext<ChartHub> chartHub)
    {
        _scopeFactory = scopeFactory;
        _alertHub = alertHub;
        _mapHub = mapHub;
        _chartHub = chartHub;
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
                } else
                {
                    sensor.SensorPositioningDate = DateTime.UtcNow;
                    _context.Sensors.Update(sensor);
                }

                _context.SensorData.Add(data);
                await _context.SaveChangesAsync();

                string state;
                if (data.Temperature > 60 || data.Smoke > 280)
                    state = "red";
                else if (data.Temperature > 42 || data.Smoke > 250)
                    state = "yellow";
                else
                    state = "green";

                sensor.SensorState = state;
                sensor.SensorDangerSituation = (state != "green");

                await _context.SaveChangesAsync();

                // ✅ إرسال التحديث إلى الخريطة
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

                // ✅ حساب عدد الحساسات لكل حالة
                var greenCount = _context.Sensors.Count(s => s.SensorState == "green");
                var yellowCount = _context.Sensors.Count(s => s.SensorState == "yellow");
                var redCount = _context.Sensors.Count(s => s.SensorState == "red");

                // ✅ إرسال التحديث إلى واجهة المخططات + الحالة + العدادات
                await _chartHub.Clients.All.SendAsync("ReceiveSensorData", data.SensorId, new
                {
                    timestamp = data.Timestamp,
                    temperature = data.Temperature,
                    humidity = data.Humidity,
                    smoke = data.Smoke
                }, state, sensor.SensorDangerSituation, greenCount, yellowCount, redCount, sensor.SensorPositioningDate);

                // ✅ إرسال تنبيه في حال كانت الحالة خطيرة
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

            await _mqttClient.SubscribeAsync("forest_fire/data");
            Console.WriteLine("Subscribed to topic: forest_fire/data");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT Connection Error: {ex.Message}");
        }
    }
}
