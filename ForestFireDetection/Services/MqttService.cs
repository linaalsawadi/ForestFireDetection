using ForestFireDetection.Helpers;
using ForestFireDetection.Models;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

namespace ForestFireDetection.Services
{
    public class MqttService : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MqttService> _logger;
        private readonly IConfiguration _configuration;
        private IMqttClient? _mqttClient;

        public MqttService(
            IServiceScopeFactory scopeFactory,
            ILogger<MqttService> logger,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ConnectAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient?.IsConnected == true)
            {
                await _mqttClient.DisconnectAsync();
                _logger.LogInformation("MQTT disconnected gracefully");
            }
        }

        private async Task ConnectAsync()
        {
            try
            {
                var mqttConfig = _configuration.GetSection("Mqtt");
                var server = mqttConfig["Server"];
                var port = int.Parse(mqttConfig["Port"] ?? "8883");
                var username = mqttConfig["Username"];
                var password = mqttConfig["Password"];
                var topic = mqttConfig["Topic"] ?? "forest_fire/data/#";

                if (string.IsNullOrEmpty(server))
                {
                    _logger.LogWarning("MQTT server not configured. Skipping MQTT connection.");
                    return;
                }

                var factory = new MqttFactory();
                _mqttClient = factory.CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(server, port)
                    .WithCredentials(username, password)
                    .WithTls()
                    .Build();

                _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

                _mqttClient.DisconnectedAsync += async e =>
                {
                    _logger.LogWarning("MQTT disconnected: {Reason}. Reconnecting in 5s...",
                        e.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    try
                    {
                        await ConnectAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "MQTT reconnection failed");
                    }
                };

                await _mqttClient.ConnectAsync(options);
                await _mqttClient.SubscribeAsync(topic);
                _logger.LogInformation("MQTT connected and subscribed to {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT connection error");
            }
        }

        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<SensorDataProcessor>();

                // Use Payload property instead of PayloadSegment
                var payloadBytes = e.ApplicationMessage.Payload;
                string base64Payload = Encoding.UTF8.GetString(payloadBytes);
                _logger.LogDebug("MQTT received for topic {Topic}", e.ApplicationMessage.Topic);

                string? decryptedRaw = AESHelper.DecryptToRawText(base64Payload);
                if (string.IsNullOrWhiteSpace(decryptedRaw))
                {
                    _logger.LogWarning("Failed to decrypt MQTT message");
                    return;
                }

                var data = JsonSerializer.Deserialize<SensorData>(decryptedRaw);
                if (data == null || string.IsNullOrEmpty(data.SensorId))
                {
                    _logger.LogWarning("Invalid sensor data received");
                    return;
                }

                data.Id = Guid.NewGuid();
                data.Timestamp = DateTime.UtcNow;

                await processor.ProcessAsync(data);
                _logger.LogDebug("Processed: Sensor={SensorId}, Temp={Temp}°C, Hum={Hum}%, Smoke={Smoke}",
                    data.SensorId, data.Temperature, data.Humidity, data.Smoke);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON decode error in MQTT handler");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in MQTT handler");
            }
        }

        public void Dispose()
        {
            _mqttClient?.Dispose();
        }
    }
}