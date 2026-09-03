using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;

namespace Peppermint.FleetManagement.Simulator;

public class Program
{
    public static async Task Main(string[] args)
    {
        string targetRobotId = Environment.GetEnvironmentVariable("ROBOT_ID") ?? string.Empty;
        string mqttHost = Environment.GetEnvironmentVariable("MQTT_BROKER_HOST") ?? "localhost";
        int mqttPort = int.TryParse(Environment.GetEnvironmentVariable("MQTT_BROKER_PORT"), out int p) ? p : 1883;

        // If no specific ROBOT_ID is assigned, act as the Orchestrator and spawn 8 separate OS processes
        if (string.IsNullOrEmpty(targetRobotId))
        {
            Console.WriteLine("[Orchestrator] Starting 8 independent robot processes...");
            string[] robotIds = ["r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8"];
            List<Process> processes = new();

            string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? "dotnet";

            foreach (var robotId in robotIds)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = currentExePath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                if (currentExePath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase) || 
                    currentExePath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                {
                    string dllPath = typeof(Program).Assembly.Location;
                    startInfo.Arguments = $"\"{dllPath}\"";
                }

                startInfo.EnvironmentVariables["ROBOT_ID"] = robotId;
                startInfo.EnvironmentVariables["MQTT_BROKER_HOST"] = mqttHost;
                startInfo.EnvironmentVariables["MQTT_BROKER_PORT"] = mqttPort.ToString();

                var process = Process.Start(startInfo);
                if (process != null)
                {
                    processes.Add(process);
                    Console.WriteLine($"[Orchestrator] Spawned Process PID {process.Id} for Robot '{robotId}'");
                }
            }

            Console.WriteLine("[Orchestrator] All 8 robot processes running. Press Ctrl+C to exit.");
            foreach (var proc in processes)
            {
                await proc.WaitForExitAsync();
            }
            return;
        }

        // Single Robot Process Execution
        Console.WriteLine($"[Robot:{targetRobotId}] Initializing simulator process...");
        
        var mqttFactory = new MqttFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttHost, mqttPort)
            .WithClientId($"Simulator_{targetRobotId}_{Guid.NewGuid().ToString("N")[..6]}")
            .WithCleanSession()
            .Build();

        bool connected = false;
        while (!connected)
        {
            try
            {
                Console.WriteLine($"[Robot:{targetRobotId}] Connecting to MQTT Broker at {mqttHost}:{mqttPort}...");
                await mqttClient.ConnectAsync(options, CancellationToken.None);
                connected = true;
                Console.WriteLine($"[Robot:{targetRobotId}] Connected to MQTT Broker.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Robot:{targetRobotId}] Connection failed: {ex.Message}. Retrying in 3s...");
                await Task.Delay(3000);
            }
        }

        string eventsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "events.jsonl");
        if (!File.Exists(eventsFilePath))
        {
            Console.WriteLine($"[Robot:{targetRobotId}] Error: events.jsonl file not found at {eventsFilePath}");
            return;
        }

        var lines = await File.ReadAllLinesAsync(eventsFilePath);
        long lastT = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            
            if (!root.TryGetProperty("robot_id", out var idProp) || idProp.GetString() != targetRobotId)
            {
                continue; // Skip events belonging to other robots
            }

            long currentT = root.GetProperty("t").GetInt64();
            long delaySeconds = currentT - lastT;
            if (delaySeconds > 0)
            {
                await Task.Delay((int)(delaySeconds * 1000)); // Sleep matching event log timestamps
            }
            lastT = currentT;

            string topic = $"robots/{targetRobotId}/telemetry";
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(line))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await mqttClient.PublishAsync(message, CancellationToken.None);
            Console.WriteLine($"[Robot:{targetRobotId}] Published telemetry @ t={currentT}");
        }

        Console.WriteLine($"[Robot:{targetRobotId}] Finished replaying events.jsonl.");
    }
}