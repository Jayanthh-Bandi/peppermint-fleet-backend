using Peppermint.FleetManagement.Domain.Models;

namespace Peppermint.FleetManagement.Application.Interfaces;

public interface IFleetStateWriteStore
{
    void InitializeFleet(IEnumerable<Robot> initialRobots);
    void UpdateTelemetry(TelemetryEvent telemetry);
}