# Written Answers - Backend Assignment

## 1. Fleet State Representation
**File Location:** `src/Peppermint.FleetManagement.Application/Services/FleetStateManager.cs`

In the backend, the fleet's current state is held in memory using a `ConcurrentDictionary<string, Robot>`. This shape was chosen to provide $O(1)$ lock-free read and write access across high-frequency asynchronous operations. 

Because both the WebSocket stream (`SignalRBroadcasterService.cs`) and the polling REST endpoint (`RobotsController.cs`) consume this same data, the state manager implements two segregated interfaces: `IFleetStateReadStore` and `IFleetStateWriteStore`. When incoming MQTT telemetry arrives, the `FleetStateManager` updates the corresponding domain model thread-safely and fires an in-memory C# event (`OnRobotUpdated`). The SignalR broadcaster handles this event to push updates instantly to WebSockets clients, while HTTP clients polling `GET /api/robots` read directly from the read store, ensuring zero data inconsistency between both access patterns.

## 2. Tradeoffs & Delivery Semantics
**File Location:** `src/Peppermint.FleetManagement.Infrastructure/Services/MqttTelemetryIngestionWorker.cs`

We selected **MQTT over TCP** with **QoS 1 (At Least Once)** delivery for robot-to-backend communication. 

**Tradeoff:** MQTT was chosen over raw HTTP POST webhooks or gRPC streams because real-world robotics platforms operate over unreliable cellular or Wi-Fi networks where connections drop frequently. MQTT provides lightweight packet headers, native keep-alive pinging, and automatic broker-side buffering. 

**Cost & Reconciliation:** QoS 1 guarantees packet delivery but introduces the possibility of duplicate telemetry messages during network reconnections. To reconcile QoS 1 duplicate delivery with our SignalR WebSocket fanout, `FleetStateManager` processes updates idempotently using monotonically increasing event timestamps (`t`). If a duplicate message arrives, the domain state is updated safely without corrupting client views, ensuring smooth real-time fanout.

## 3. Scope Exclusions & Next Engineering Steps
**File Location:** `src/Peppermint.FleetManagement.Api/Program.cs`

**Left Out:** 
1. Persistent database history store (e.g., PostgreSQL / TimescaleDB) for long-term spatial query analysis.
2. Authentication and authorization layers (OAuth2 / JWT) on the WebSocket hub and REST endpoints.

**What to Build Next:** 
Given additional development time, I would integrate a time-series store like **TimescaleDB** or **SQLite** to persist historical coordinates, exposing `GET /api/robots/history/{robot_id}?start={t1}&end={t2}`. I would also add a Redis Backplane to scale SignalR WebSockets horizontally across multiple API container instances.