using System.Text.Json;
using Peppermint.FleetManagement.Api.Hubs;
using Peppermint.FleetManagement.Api.Services;
using Peppermint.FleetManagement.Application.Interfaces;
using Peppermint.FleetManagement.Application.Services;
using Peppermint.FleetManagement.Domain.Models;
using Peppermint.FleetManagement.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Services & DI
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Register Thread-Safe Fleet State Manager as a Singleton
var stateManager = new FleetStateManager();
builder.Services.AddSingleton<IFleetStateReadStore>(stateManager);
builder.Services.AddSingleton<IFleetStateWriteStore>(stateManager);

// Register Background Hosted Services
builder.Services.AddHostedService<MqttTelemetryIngestionWorker>();
builder.Services.AddHostedService<SignalRBroadcasterService>();

// Configure CORS for external Web Dashboard clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true)
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed initial fleet positions from robots.json
SeedInitialFleet(stateManager);

app.UseCors("AllowAll");
app.MapControllers();
app.MapHub<FleetHub>("/hubs/fleet");

app.Run();

static void SeedInitialFleet(IFleetStateWriteStore writeStore)
{
    string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "robots.json");
    if (File.Exists(jsonPath))
    {
        string json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);
        var initialRobots = new List<Robot>();

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            string id = element.GetProperty("robot_id").GetString()!;
            string type = element.GetProperty("robot_type").GetString()!;
            var start = element.GetProperty("start");
            double x = start.GetProperty("x").GetDouble();
            double y = start.GetProperty("y").GetDouble();

            initialRobots.Add(new Robot(id, type, new Position(x, y)));
        }

        writeStore.InitializeFleet(initialRobots);
    }
}