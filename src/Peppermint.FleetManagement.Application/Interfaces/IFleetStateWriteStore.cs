using Peppermint.FleetManagement.Application.DTOs;
using Peppermint.FleetManagement.Domain.Models;

namespace Peppermint.FleetManagement.Application.Interfaces;

public interface IFleetStateWriteStore
{
    event Action<RobotDto>? OnRobotUpdated;
    void InitializeFleet(IEnumerable<Robot> initialRobots);
    void UpdateTelemetry(TelemetryEvent telemetry);
}