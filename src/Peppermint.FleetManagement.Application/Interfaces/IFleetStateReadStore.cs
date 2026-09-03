using Peppermint.FleetManagement.Application.DTOs;

namespace Peppermint.FleetManagement.Application.Interfaces;

public interface IFleetStateReadStore
{
    IReadOnlyCollection<RobotDto> GetAllRobots();
    RobotDto? GetRobotById(string robotId);
}