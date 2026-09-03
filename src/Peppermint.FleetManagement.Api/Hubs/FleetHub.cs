using Microsoft.AspNetCore.SignalR;
using Peppermint.FleetManagement.Application.Interfaces;

namespace Peppermint.FleetManagement.Api.Hubs;

public class FleetHub : Hub
{
    private readonly IFleetStateReadStore _readStore;

    public FleetHub(IFleetStateReadStore readStore)
    {
        _readStore = readStore;
    }

    public override async Task OnConnectedAsync()
    {
        // On connection, immediately push the entire current fleet snapshot to the new client
        var currentFleet = _readStore.GetAllRobots();
        await Clients.Caller.SendAsync("FleetSnapshot", currentFleet);
        await base.OnConnectedAsync();
    }
}