using Peppermint.FleetManagement.Domain.Enums;

namespace Peppermint.FleetManagement.Domain.Models;

public record TelemetryEvent(
    long Timestamp,
    string RobotId,
    double X,
    double Y,
    RobotStatus Status,
    double Battery,
    string? TaskEvent = null
);