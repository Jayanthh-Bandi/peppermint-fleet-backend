using Microsoft.AspNetCore.Mvc;
using Peppermint.FleetManagement.Application.DTOs;
using Peppermint.FleetManagement.Application.Interfaces;

namespace Peppermint.FleetManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RobotsController : ControllerBase
{
    private readonly IFleetStateReadStore _readStore;

    public RobotsController(IFleetStateReadStore readStore)
    {
        _readStore = readStore;
    }

    [HttpGet]
    public ActionResult<IEnumerable<RobotDto>> GetAllRobots()
    {
        return Ok(_readStore.GetAllRobots());
    }

    [HttpGet("{id}")]
    public ActionResult<RobotDto> GetRobotById(string id)
    {
        var robot = _readStore.GetRobotById(id);
        if (robot == null)
        {
            return NotFound(new { Message = $"Robot with ID '{id}' was not found." });
        }
        return Ok(robot);
    }
}