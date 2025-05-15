using ForestFireDetection.Helpers;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ForestFireDetection.Models;
using ForestFireDetection.Services;

namespace ForestFireDetection.Services
{
    public class MqttService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private IMqttClient _mqttClient;

        public MqttService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
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
                    var processor = scope.ServiceProvider.GetRequiredService<SensorDataProcessor>();

                    // ✅ تم التعديل: تحويل الحمولة إلى نص Base64
                    string base64Payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                    // ✅ تم التعديل: فك تشفير AES واستخراج JSON
                    var (jsonDecrypted, rawText) = AESHelper.DecryptBase64(base64Payload);

                    //var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    //var data = JsonSerializer.Deserialize<SensorData>(payload);

                    Console.WriteLine($"\n📦 Base64: {base64Payload}"); // ✅ عرض الـ Base64
                    Console.WriteLine($"🔓 RAW: {rawText}");             // ✅ عرض النص المفكوك

                    // ✅ التحقق من نجاح فك التشفير
                    if (jsonDecrypted == null)
                    {
                        Console.WriteLine("❌ Failed to decrypt or extract JSON.");
                        return;
                    }

                    // ✅ تم التعديل: تحويل JSON إلى كائن SensorData
                    var data = JsonSerializer.Deserialize<SensorData>(jsonDecrypted);
                    if (data == null)
                    {
                        Console.WriteLine("⚠️ JSON deserialization failed.");
                        return;
                    }


                    //if (data == null) return;

                    data.Id = Guid.NewGuid();
                    data.Timestamp = DateTime.UtcNow;

                    await processor.ProcessAsync(data);

                    // ✅ تم الإضافة: تأكيد البيانات المعالجة
                    Console.WriteLine($"✅ SensorData: Temp={data.Temperature}, Hum={data.Humidity}, Smo={data.Smoke}");

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

                //await _mqttClient.SubscribeAsync("forest_fire/sensor");
                //Console.WriteLine("Subscribed to topic: forest_fire/sensor");

                // ✅ تم التعديل: topic الصحيح
                await _mqttClient.SubscribeAsync("forest_fire/data/#");
                Console.WriteLine("Subscribed to topic: forest_fire/data/#");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT Connection Error: {ex.Message}");
            }
        }
    }
}
