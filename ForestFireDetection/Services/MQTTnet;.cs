using MQTTnet;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using ForestFireDetection.Data;
using ForestFireDetection.Models;
using ForestFireDetection.Hubs;
using System.Threading.Tasks;

public class MqttService
{
    private readonly ForestFireDetectionDbContext _context;
    private readonly IHubContext<AlertHub> _hubContext;
    private IMqttClient _mqttClient;

    public MqttService(ForestFireDetectionDbContext context, IHubContext<AlertHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task StartAsync()
    {
        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("broker.hivemq.com", 1883)
            .Build();

        try
        {
            // الاتصال بالـ Broker
            await _mqttClient.ConnectAsync(options);
            Console.WriteLine("Connected to MQTT Broker successfully.");

            // الاشتراك في الموضوع بعد الاتصال
            await _mqttClient.SubscribeAsync("forest/sensors");

            // التعامل مع الرسائل المستلمة
            _mqttClient.ApplicationMessageReceivedAsync += async (e) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    var data = JsonSerializer.Deserialize<SensorData>(json);

                    if (data == null) return;

                    // حفظ البيانات في قاعدة البيانات
                    data.Id = Guid.NewGuid();
                    data.Timestamp = DateTime.UtcNow;
                    _context.SensorData.Add(data);
                    await _context.SaveChangesAsync();

                    // تحليل البيانات
                    if (data.Temperature > 42 || data.Smoke > 50)
                    {
                        var alarm = new Alert
                        {
                            Id = Guid.NewGuid(),
                            SensorId = data.SensorId,
                            Temperature = data.Temperature,
                            Smoke = data.Smoke,
                            Timestamp = DateTime.UtcNow,
                            Status = "NotReviewed",
                            Latitude = data.Latitude, // إضافة الموقع الجغرافي إذا كان موجود
                            Longitude = data.Longitude
                        };

                        _context.Alerts.Add(alarm);
                        await _context.SaveChangesAsync();

                        // إرسال الإنذار عبر SignalR
                        await _hubContext.Clients.All.SendAsync("NewAlarm", new
                        {
                            alarm.Id,
                            alarm.SensorId,
                            alarm.Temperature,
                            alarm.Smoke,
                            alarm.Timestamp,
                            Location = new { alarm.Latitude, alarm.Longitude },
                            Status = alarm.Status
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing MQTT message: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to MQTT Broker: {ex.Message}");
        }
    }
}
