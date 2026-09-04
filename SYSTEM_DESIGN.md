# System Design Analysis

## 1. Extensibility & Adding New Features
**File Reference:** `src/Peppermint.FleetManagement.Domain/Models/Robot.cs` and `src/Peppermint.FleetManagement.Application/DTOs/RobotDto.cs`

The system uses Clean Architecture, keeping domain logic completely isolated from transport layers. To add a new feature—such as reporting internal motor temperature or error diagnostics—we would modify the `TelemetryEvent` record and update `Robot.cs` to store the new state property. Because `RobotsController` and `FleetHub` consume `RobotDto`, we expose the new field in `RobotDto.cs`. The MQTT ingestion worker automatically parses the extended JSON fields without requiring architectural re-engineering.

## 2. Scalability Bottlenecks (8 to 500 Robots)
**File Reference:** `src/Peppermint.FleetManagement.Application/Services/FleetStateManager.cs` and `src/Peppermint.FleetManagement.Api/Services/SignalRBroadcasterService.cs`

If the fleet expands from 8 to 500 robots publishing telemetry every second, the first bottleneck will be **SignalR WebSocket Fanout CPU & Network I/O**. Broadcasting 500 individual state updates per second across hundreds of connected browser clients creates $500 \times N_{\text{clients}}$ messages per second, saturating single-node thread pools.

**Resolution:** 
1. Replace in-memory event publishing with a **Redis Pub/Sub Backplane** or **Apache Kafka** topic.
2. Throttle real-time WebSocket broadcasts by batching updates into 100ms interval snapshots rather than emitting an event per packet.

## 3. Bandwidth Optimization Strategies
**File Reference:** `src/Peppermint.FleetManagement.Infrastructure/Services/MqttTelemetryIngestionWorker.cs`

When network bandwidth between robots and the backend is strictly constrained:
1. **Delta Updates:** Modify telemetry payloads to send position coordinates only when the delta exceeds a spatial threshold ($\Delta x > 0.5\text{m}$), omitting unchanged static fields like `robot_type`.
2. **Binary Serialization:** Replace JSON payload strings with **Protocol Buffers (Protobuf)** or MessagePack. Protobuf compresses telemetry down to ~15-20 bytes per update compared to ~100 bytes for raw JSON.
3. **Adaptive Heartbeats:** Reduce telemetry output frequency while robots are in `Idle` or `Charging` status from 5s to 30s.

## 4. Handling Unresponsive & Crashed Robots
**File Reference:** `src/Peppermint.FleetManagement.Application/Services/FleetStateManager.cs`

If a robot crashes mid-task, it stops sending telemetry. 

**Detection & Action:**
We would implement a background Hosted Service (`HeartbeatHealthCheckWorker`) in the backend that sweeps `FleetStateManager` every 5 seconds. If `CurrentTimestamp - LastUpdatedTimestamp > 15s`, the health worker transitions the robot's status to `Offline` or `Error` automatically, triggering an `OnRobotUpdated` event to alert dashboard operators via WebSockets immediately.

## 5. Unreliable Networks, Out-of-Order Updates, and Recovery
**File Reference:** `src/Peppermint.FleetManagement.Domain/Models/Robot.cs`

On cellular/Wi-Fi networks, updates can arrive late or out-of-order.

**System Behavior & Recovery:**
1. Each event payload carries a monotonic source timestamp `t`.
2. Inside `Robot.UpdateTelemetry()`, the entity validates timestamps: if incoming telemetry has $t_{\text{incoming}} < t_{\text{last\_updated}}$, the out-of-order update is safely discarded to prevent coordinate regression.
3. During disconnections, robots queue updates in local flash memory. Once connection recovers, buffered telemetry is flushed to the backend. The backend updates current state using the newest timestamp while storing historical packets sequentially.