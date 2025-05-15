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
        private const string Topic = "forest_fire/data/#";

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

                    string base64Payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    Console.WriteLine($"📦 MQTT Base64: {base64Payload}");

                    string? decryptedRaw = AESHelper.DecryptToRawText(base64Payload);

                    if (string.IsNullOrWhiteSpace(decryptedRaw))
                    {
                        Console.WriteLine("❌ Failed to decrypt message.");
                        return;
                    }

                    // قص كل ما قبل الجملة "temp"
                    int jsonStart = decryptedRaw.IndexOf("\"temp\"");
                    int jsonEnd = decryptedRaw.LastIndexOf('}');

                    if (jsonStart >= 0 && jsonEnd > jsonStart)
                    {
                        // نضيف { من البداية التي تم قطعها
                        string jsonBlock = "{" + decryptedRaw.Substring(jsonStart, jsonEnd - jsonStart + 1);
                        Console.WriteLine($"🟨 Cleaned JSON Block: {jsonBlock}");

                        try
                        {
                            var data = JsonSerializer.Deserialize<SensorData>(jsonBlock);
                            if (data == null || data.SensorId == Guid.Empty)
                            {
                                Console.WriteLine("⚠️ JSON deserialization failed or SensorId missing.");
                                return;
                            }

                            data.Id = Guid.NewGuid();
                            data.Timestamp = DateTime.UtcNow;

                            await processor.ProcessAsync(data);
                            Console.WriteLine($"✅ Decrypted JSON: Temp={data.Temperature}, Hum={data.Humidity}, Smo={data.Smoke}");
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("⚠️ JSON decode error.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ No valid JSON block found.");
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
                Console.WriteLine("✅ Connected to HiveMQ broker.");
                await _mqttClient.SubscribeAsync(Topic);
                Console.WriteLine($"📡 Subscribed to topic: {Topic}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MQTT Connection Error: {ex.Message}");
            }
        }
    }
}
