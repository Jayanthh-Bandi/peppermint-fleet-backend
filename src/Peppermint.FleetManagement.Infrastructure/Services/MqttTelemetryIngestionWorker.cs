using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using Peppermint.FleetManagement.Application.Interfaces;
using Peppermint.FleetManagement.Domain.Enums;
using Peppermint.FleetManagement.Domain.Models;

namespace Peppermint.FleetManagement.Infrastructure.Services;

public class MqttTelemetryIngestionWorker : BackgroundService
{
    private readonly IFleetStateWriteStore _stateStore;
    private readonly ILogger<MqttTelemetryIngestionWorker> _logger;

    public MqttTelemetryIngestionWorker(
        IFleetStateWriteStore stateStore,
        ILogger<MqttTelemetryIngestionWorker> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string mqttHost = Environment.GetEnvironmentVariable("MQTT_BROKER_HOST") ?? "localhost";
        int mqttPort = int.TryParse(Environment.GetEnvironmentVariable("MQTT_BROKER_PORT"), out int p) ? p : 1883;

        var mqttFactory = new MqttFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttHost, mqttPort)
            .WithClientId($"BackendWorker_{Guid.NewGuid().ToString("N")[..6]}")
            .WithCleanSession()
            .Build();

        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            try
            {
                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                string robotId = root.GetProperty("robot_id").GetString()!;
                long t = root.GetProperty("t").GetInt64();
                double x = root.GetProperty("x").GetDouble();
                double y = root.GetProperty("y").GetDouble();
                double battery = root.GetProperty("battery").GetDouble();
                string rawStatus = root.GetProperty("status").GetString()!;

                if (Enum.TryParse<RobotStatus>(NormalizeStatus(rawStatus), true, out var parsedStatus))
                {
                    string? taskEvent = root.TryGetProperty("task_event", out var te) ? te.GetString() : null;

                    var telemetry = new TelemetryEvent(t, robotId, x, y, parsedStatus, battery, taskEvent);
                    _stateStore.UpdateTelemetry(telemetry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing incoming MQTT telemetry message.");
            }

            return Task.CompletedTask;
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Connecting MQTT Telemetry Ingestion Worker to broker {Host}:{Port}...", mqttHost, mqttPort);
                await mqttClient.ConnectAsync(options, stoppingToken);

                await mqttClient.SubscribeAsync("robots/+/telemetry", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, stoppingToken);
                _logger.LogInformation("Subscribed to MQTT topic 'robots/+/telemetry'.");

                while (mqttClient.IsConnected && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("MQTT Broker connection error: {Message}. Reconnecting in 3s...", ex.Message);
                await Task.Delay(3000, stoppingToken);
            }
        }
    }

    private static string NormalizeStatus(string rawStatus)
    {
        return rawStatus.Replace("_", string.Empty);
    }
}