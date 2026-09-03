using Peppermint.FleetManagement.Application.Services;
using Peppermint.FleetManagement.Domain.Enums;
using Peppermint.FleetManagement.Domain.Models;
using Xunit;

namespace Peppermint.FleetManagement.Tests;

public class FleetStateManagerTests
{
    private readonly FleetStateManager _stateManager = new();

    [Fact]
    public void InitializeFleet_PopulatesAllRobotsCorrectly()
    {
        // Arrange
        var initialRobots = new List<Robot>
        {
            new("r1", "picker", new Position(10, 20)),
            new("r2", "hauler", new Position(30, 40))
        };

        // Act
        _stateManager.InitializeFleet(initialRobots);
        var fleet = _stateManager.GetAllRobots();

        // Assert
        Assert.Equal(2, fleet.Count);
        var robot1 = _stateManager.GetRobotById("r1");
        Assert.NotNull(robot1);
        Assert.Equal("picker", robot1.RobotType);
        Assert.Equal(10, robot1.X);
        Assert.Equal(20, robot1.Y);
        Assert.Equal("Idle", robot1.Status);
    }

    [Fact]
    public void UpdateTelemetry_UpdatesRobotPositionAndStatusSuccessfully()
    {
        // Arrange
        var initialRobots = new List<Robot>
        {
            new("r1", "picker", new Position(0, 0))
        };
        _stateManager.InitializeFleet(initialRobots);

        var telemetry = new TelemetryEvent(
            Timestamp: 55,
            RobotId: "r1",
            X: 105.5,
            Y: 200.2,
            Status: RobotStatus.OnMission,
            Battery: 85.4,
            TaskEvent: "task_started"
        );

        // Act
        _stateManager.UpdateTelemetry(telemetry);
        var updatedRobot = _stateManager.GetRobotById("r1");

        // Assert
        Assert.NotNull(updatedRobot);
        Assert.Equal(105.5, updatedRobot.X);
        Assert.Equal(200.2, updatedRobot.Y);
        Assert.Equal("OnMission", updatedRobot.Status);
        Assert.Equal(85.4, updatedRobot.BatteryPercentage);
        Assert.Equal(55, updatedRobot.LastUpdatedTimestamp);
        Assert.Equal("task_started", updatedRobot.LastTaskEvent);
    }

    [Fact]
    public void ConcurrentTelemetryUpdates_MaintainsStateConsistencyWithoutExceptions()
    {
        // Arrange
        var initialRobots = new List<Robot>
        {
            new("r1", "picker", new Position(0, 0))
        };
        _stateManager.InitializeFleet(initialRobots);

        // Act: Simulate 100 concurrent thread updates on the same robot
        Parallel.For(0, 100, i =>
        {
            var telemetry = new TelemetryEvent(
                Timestamp: i,
                RobotId: "r1",
                X: i * 1.0,
                Y: i * 2.0,
                Status: RobotStatus.Active,
                Battery: 100.0 - (i * 0.1)
            );
            _stateManager.UpdateTelemetry(telemetry);
        });

        var finalRobot = _stateManager.GetRobotById("r1");

        // Assert
        Assert.NotNull(finalRobot);
        Assert.Equal("Active", finalRobot.Status);
        Assert.True(finalRobot.BatteryPercentage <= 100.0);
    }
}