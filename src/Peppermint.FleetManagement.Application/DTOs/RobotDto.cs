namespace Peppermint.FleetManagement.Application.DTOs;

public record RobotDto(
    string RobotId,
    string RobotType,
    double X,
    double Y,
    double BatteryPercentage,
    string Status,
    long LastUpdatedTimestamp,
    string? LastTaskEvent
);