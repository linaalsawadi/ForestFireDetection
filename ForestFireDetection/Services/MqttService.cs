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

                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    var data = JsonSerializer.Deserialize<SensorData>(payload);

                    if (data == null) return;

                    data.Id = Guid.NewGuid();
                    data.Timestamp = DateTime.UtcNow;

                    await processor.ProcessAsync(data);
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

                await _mqttClient.SubscribeAsync("forest_fire/sensor");
                Console.WriteLine("Subscribed to topic: forest_fire/sensor");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT Connection Error: {ex.Message}");
            }
        }
    }
}
