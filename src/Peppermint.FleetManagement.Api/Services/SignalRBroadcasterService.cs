using Microsoft.AspNetCore.SignalR;
using Peppermint.FleetManagement.Api.Hubs;
using Peppermint.FleetManagement.Application.DTOs;
using Peppermint.FleetManagement.Application.Interfaces;

namespace Peppermint.FleetManagement.Api.Services;

public class SignalRBroadcasterService : IHostedService
{
    private readonly IFleetStateWriteStore _writeStore;
    private readonly IHubContext<FleetHub> _hubContext;

    public SignalRBroadcasterService(
        IFleetStateWriteStore writeStore,
        IHubContext<FleetHub> hubContext)
    {
        _writeStore = writeStore;
        _hubContext = hubContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _writeStore.OnRobotUpdated += OnRobotUpdated;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _writeStore.OnRobotUpdated -= OnRobotUpdated;
        return Task.CompletedTask;
    }

    private void OnRobotUpdated(RobotDto robotDto)
    {
        _hubContext.Clients.All.SendAsync("RobotUpdated", robotDto);
    }
}