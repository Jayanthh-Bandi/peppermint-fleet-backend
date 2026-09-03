using Peppermint.FleetManagement.Domain.Enums;

namespace Peppermint.FleetManagement.Domain.Models;

public class Robot
{
    public string RobotId { get; private set; } = string.Empty;
    public string RobotType { get; private set; } = string.Empty;
    public Position CurrentPosition { get; private set; } = new(0, 0);
    public double BatteryPercentage { get; private set; }
    public RobotStatus Status { get; private set; } = RobotStatus.Offline;
    public long LastUpdatedTimestamp { get; private set; }
    public string? LastTaskEvent { get; private set; }

    public Robot(string robotId, string robotType, Position initialPosition)
    {
        RobotId = robotId;
        RobotType = robotType;
        CurrentPosition = initialPosition;
        BatteryPercentage = 100.0;
        Status = RobotStatus.Idle;
        LastUpdatedTimestamp = 0;
    }

    public void UpdateTelemetry(TelemetryEvent telemetry)
    {
        CurrentPosition = new Position(telemetry.X, telemetry.Y);
        BatteryPercentage = telemetry.Battery;
        Status = telemetry.Status;
        LastUpdatedTimestamp = telemetry.Timestamp;
        
        if (!string.IsNullOrEmpty(telemetry.TaskEvent))
        {
            LastTaskEvent = telemetry.TaskEvent;
        }
    }
}