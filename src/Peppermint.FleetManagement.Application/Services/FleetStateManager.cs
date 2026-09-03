using System.Collections.Concurrent;
using Peppermint.FleetManagement.Application.DTOs;
using Peppermint.FleetManagement.Application.Interfaces;
using Peppermint.FleetManagement.Domain.Models;

namespace Peppermint.FleetManagement.Application.Services;

public class FleetStateManager : IFleetStateReadStore, IFleetStateWriteStore
{
    private readonly ConcurrentDictionary<string, Robot> _robots = new();

    public event Action<RobotDto>? OnRobotUpdated;

    public void InitializeFleet(IEnumerable<Robot> initialRobots)
    {
        foreach (var robot in initialRobots)
        {
            _robots.TryAdd(robot.RobotId, robot);
        }
    }

    public void UpdateTelemetry(TelemetryEvent telemetry)
    {
        if (_robots.TryGetValue(telemetry.RobotId, out var robot))
        {
            robot.UpdateTelemetry(telemetry);
            var dto = MapToDto(robot);
            OnRobotUpdated?.Invoke(dto);
        }
    }

    public IReadOnlyCollection<RobotDto> GetAllRobots()
    {
        return _robots.Values.Select(MapToDto).ToList();
    }

    public RobotDto? GetRobotById(string robotId)
    {
        return _robots.TryGetValue(robotId, out var robot) ? MapToDto(robot) : null;
    }

    private static RobotDto MapToDto(Robot robot) =>
        new(
            robot.RobotId,
            robot.RobotType,
            robot.CurrentPosition.X,
            robot.CurrentPosition.Y,
            robot.BatteryPercentage,
            robot.Status.ToString(),
            robot.LastUpdatedTimestamp,
            robot.LastTaskEvent
        );
}