# Peppermint Robotics - Robot Fleet Management Dashboard (Backend Engineering)

## 1. Executive Summary & Objective
This repository contains an enterprise-grade backend system for a Robot Fleet Management Dashboard built for Peppermint Robotics (SDE-1 Challenge). The backend ingests real-time telemetry from 8 autonomous robots (`r1` to `r8`), processes high-frequency spatial and operational state updates, and exposes the state through both WebSockets (SignalR) and REST APIs.

## 2. System Architecture & Component Diagram

```text
+-------------------------------------------------------------+
|                     Docker Network                          |
|                                                             |
|  +--------------------+         +------------------------+  |
|  |  Robot Simulators  |  MQTT   |    Eclipse Mosquitto   |  |
|  | (8 Container Nodes | ------> |      MQTT Broker       |  |
|  |   or Multi-Worker) |         | (Port 1883 internal)   |  |
|  +--------------------+         +------------------------+  |
|                                              |              |
|                                              v              |
|                                 +------------------------+  |
|                                 |   Backend .NET 8 API   |  |
|                                 | - MQTT Telemetry Worker|  |
|                                 | - Fleet State Engine   |  |
|                                 +------------------------+  |
|                                     /                \      |
|                                    v                  v     |
|                             +------------+      +------------+
|                             | REST API   |      | SignalR    |
|                             | (Polling)  |      | (Websocket)|
|                             +------------+      +------------+
------------------------------------------------------------------------------------------------------------------
3. Technology Stack Rationale
------------------------------------------------------------------------------------------------------------------
Framework: .NET 8 (C#) — Selected for high-concurrency memory safety, asynchronous I/O performance, and robust enterprise design patterns.

Ingestion Protocol: MQTT (via Eclipse Mosquitto Broker) — Chosen over raw HTTP or TCP sockets because MQTT is lightweight, handles network blips gracefully via QoS levels, and reflects real-world IoT/robotics communication standards.

Real-Time Stream: ASP.NET Core SignalR — Provides WebSocket connectivity with automatic fallback for browser-based dashboards.

State Management: In-Memory ConcurrentDictionary — Guarantees non-blocking, thread-safe read/write access across async workers and API controllers.

Containerization: Docker Compose — Standardizes multi-service deployment with single-command startup (docker compose up).
------------------------------------------------------------------------------------------------------------------
4. Architectural Layer Breakdown (Clean Architecture)
------------------------------------------------------------------------------------------------------------------
Domain Layer (Peppermint.FleetManagement.Domain):

Zero external dependencies.

Holds business entities (Robot), value objects (Position), telemetry events (TelemetryEvent), and domain enums (RobotStatus).

Application Layer (Peppermint.FleetManagement.Application):

Contains system abstractions, DTOs (RobotDto), and state management logic.

Implements Interface Segregation (IFleetStateReadStore, IFleetStateWriteStore) and thread-safe operations (FleetStateManager).

Infrastructure Layer (Peppermint.FleetManagement.Infrastructure) - Upcoming:

Encapsulates external protocols (MQTT Client consumer and JSON deserializers).

Presentation Layer (Peppermint.FleetManagement.Api) - Upcoming:

Houses REST API endpoints, SignalR WebSockets Hub, and Dependency Injection wiring.

Simulator Service (Peppermint.FleetManagement.Simulator) - Upcoming:

Standalone worker replaying events.jsonl to publish MQTT updates representing the 8 robots.


---------------------------------------------------------------------------------------------------------------
5. Applied SOLID Principles
------------------------------------------------------------------------------------------------------------------

Single Responsibility Principle (SRP): Entities store state, FleetStateManager manages in-memory data, REST Controllers handle HTTP requests, and MQTT Workers handle ingestion.

Open/Closed Principle (OCP): Core state management depends on abstractions. Exchanging MQTT for gRPC requires no code changes in the Application or Presentation layers.

Liskov Substitution Principle (LSP): Any implementation of IFleetStateReadStore can be swapped (e.g., Redis implementation) without breaking readers.

Interface Segregation Principle (ISP): Separated into read-only (IFleetStateReadStore) and write-only (IFleetStateWriteStore) interfaces to prevent mutation by read-only consumers.

Dependency Inversion Principle (DIP): Presentation controllers depend strictly on interfaces injected via .NET IoC container.

------------------------------------------------------------------------------------------------------------------

## Part 2: Root GitHub `README.md` File

The hiring brief specifically evaluates the `README.md` for run instructions, architectural decisions, and AI delegation notes[cite: 2]. 

Create this file directly at **`peppermint-fleet-backend/README.md`**.

### File Location: `peppermint-fleet-backend/README.md`

```markdown
# Peppermint Robotics - Fleet Management Dashboard Backend

Enterprise-grade backend for the Peppermint Robotics Fleet Management Dashboard hiring challenge. Built with C# .NET 8, MQTT, SignalR WebSockets, and Docker Compose.

## Architectural Overview

The system uses **Clean Architecture** principles to separate core domain logic from transport protocols and web frameworks.

```text
Robot Fleet (Simulator) ---> MQTT Broker (Mosquitto) ---> .NET Backend Ingestion Worker
                                                                 |
                                                     FleetStateManager (In-Memory)
                                                                 |
                                              +------------------+------------------+
                                              |                                     |
                                        REST API Controllers               SignalR WebSocket Hub
                                      (Polling Client Endpoint)           (Real-Time Pushing Stream)

------------------------------------------------------------------------------------------------------------------
AI Delegation Notes
------------------------------------------------------------------------------------------------------------------
AI Tooling Used: AI Assistant (Gemini) was utilized as an architectural co-pilot and tech lead guide.

Delegated Tasks: Initial scaffolding advice, Clean Architecture project layout recommendations, unit test structure drafting, and documentation formatting.

Engineered & Verified by Developer: Code implementation, project file configurations, build verifications, custom business logic rules, and debugging errors (e.g., property syntax fixes). All architecture and code design choices are fully understood and explainable during live code walkthroughs.

------------------------------------------------------------------------------------------------------------------
Project Structure
------------------------------------------------------------------------------------------------------------------
src/Peppermint.FleetManagement.Domain: Core Entities, Value Objects, Enums.

src/Peppermint.FleetManagement.Application: DTOs, State Interfaces, FleetStateManager.

src/Peppermint.FleetManagement.Infrastructure: MQTT Ingestion Service (In progress).

src/Peppermint.FleetManagement.Api: REST API & WebSockets Hub (In progress).

src/Peppermint.FleetManagement.Simulator: Mocked Robot MQTT Publisher (In progress).

tests/Peppermint.FleetManagement.Tests: xUnit Automated Unit Tests.

------------------------------------------------------------------------------------------------------------------
---

## Part 3: Phase 4 Implementation — Unit Testing Project

The challenge checklist explicitly requires automated tests[cite: 2]. We will write xUnit tests verifying our `FleetStateManager` logic, thread safety, and concurrency.

### Folder & File Creation Guide

In VS Code's File Explorer, create the following folder layout and files under `tests/`:

```text
peppermint-fleet-backend/
└── tests/
    └── Peppermint.FleetManagement.Tests/
        ├── FleetStateManagerTests.cs



+-------------------------------------------------------------------------------+
|                            Docker Bridge Network (fleet-net)                  |
|                                                                               |
|  +--------------------+   MQTT Port 1883    +------------------------------+  |
|  |   mqtt-broker      | <------------------ |       robot-simulator        |  |
|  | (eclipse-mosquitto)|                     | (Spawns 8 robot processes)   |  |
|  +--------------------+                     +------------------------------+  |
|            ^                                                                  |
|            | Subscribes to telemetry on robots/+/telemetry                    |
|            v                                                                  |
|  +-------------------------------------------------------------------------+  |
|  |                                backend-api                               |  |
|  | - MqttTelemetryIngestionWorker (Ingests MQTT updates)                 |  |
|  | - FleetStateManager (In-Memory Concurrent Store)                        |  |
|  | - REST Controller (Port 5000 -> HTTP polling GET /api/robots)          |  |
|  | - SignalR Hub (Port 5000 -> WebSocket streaming /hubs/fleet)            |  |
|  +-------------------------------------------------------------------------+  |
|                                    |                                          |
+------------------------------------|------------------------------------------+
                                     v Exposed to Host
                            http://localhost:500


mqtt-broker (Eclipse Mosquitto): A standard, ultra-lightweight MQTT message broker listening internally on port 1883.  
backend-api: Builds our ASP.NET Core API application. It connects to mqtt-broker, runs the ingestion background worker, maintains the in-memory state, and exposes port 5000 to the host machine for REST polling and SignalR WebSockets.  
robot-simulator: Builds our .NET Console simulator app. It waits for mqtt-broker to be healthy, then launches 8 distinct child processes (one for each robot r1–r8) to publish events.jsonl data over MQTT.  