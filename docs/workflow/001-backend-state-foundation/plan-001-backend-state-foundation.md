# Backend State Foundation

## Goal

Establish the backend-driven architecture foundation by creating the AgOpenGPS.Api backend that broadcasts state updates to the WinForms frontend via SignalR at 10 Hz. This proves the architecture works and sets the foundation for incremental migration of business logic.

## Current State

- FormGPS runs legacy timers (`tmrWatchdog` at 250ms, `timerSim` at 93ms)
- All application logic runs in WinForms UI thread
- No backend separation
- No real-time state broadcasting infrastructure
- .NET Framework 4.8 (Windows-only)

## Target State

- AgOpenGPS.Api backend (.NET 8) runs ApplicationOrchestrator at 10 Hz (100ms)
- StateHub broadcasts state updates via SignalR over HTTP/WebSockets
- FormGPS receives state updates from backend (replaces legacy timers)
- Two separate processes: AgOpenGPS.Api.exe and FormGPS.exe
- Foundation ready for incremental business logic migration

## Why

- **Backend-driven architecture**: Backend owns timing and orchestration, frontend becomes passive receiver
- **Cross-platform foundation**: .NET 8 backend can run on Windows, Linux, macOS
- **Testable**: Backend can run headless without UI coupling
- **Evolutionary**: Start simple (heartbeat), grow incrementally (add GPS, guidance, sections later)
- **Future-proof**: Easy to replace WinForms with Electron/React frontend later (just change SignalR URL)

## What This Is NOT

- **NOT migrating business logic yet**: GPS parsing, guidance, section control stay in FormGPS for now
- **NOT creating service interfaces**: IGuidanceService, ISectionControlService, etc. come in later workflows
- **NOT adding feature flags**: Simple replacement, not A/B testing (keep scope small)
- **NOT optimizing state payload**: Simple counter/timestamp for now, rich state comes later

## Migration Path

This workflow establishes the foundation using the **Backend-Driven with Publisher-Subscriber** pattern:

### Phase 1: Foundation (This Workflow)
1. Create backend API project (AgOpenGPS.Api)
2. Create client library (AgOpenGPS.Api.Client)
3. Create ApplicationOrchestrator (10 Hz timer)
4. Create StateHub (SignalR broadcaster)
5. Connect FormGPS to backend, disable legacy timers
6. Verify state updates flow end-to-end

### Phase 2: Add Services (Future Workflows)
1. Add GNSS service to backend (parse GPS data)
2. Add Guidance service (AB lines, curves, headlands)
3. Add Section Control service
4. Enrich state updates with real data

### Phase 3: Migrate Logic (Future Workflows)
1. Move timer logic from FormGPS into backend services
2. FormGPS becomes pure UI renderer
3. Full strangler fig migration complete

## Tasks

1. [task1-create-api-project.md](task1-create-api-project.md) - Create AgOpenGPS.Api project (.NET 8 Web API)
2. [task2-create-client-library.md](task2-create-client-library.md) - Create ApplicationState model, IStateSubscriber interface, SignalRStateSubscriber
3. [task3-create-application-orchestrator.md](task3-create-application-orchestrator.md) - Create ApplicationOrchestrator using IStatePublisher abstraction
4. [task4-create-state-hub.md](task4-create-state-hub.md) - Create IStatePublisher interface and SignalRStatePublisher implementation
5. [task5-connect-formgps.md](task5-connect-formgps.md) - Connect FormGPS using IStateSubscriber, disable legacy timers
6. [task6-update-documentation.md](task6-update-documentation.md) - Update CLAUDE.md and architecture docs

## State Model

**ApplicationState** - Strongly-typed state object shared between backend and frontend:

```csharp
public class ApplicationState
{
    public DateTime Timestamp { get; set; }
}
```

**Location**: `AgOpenGPS.Api.Client/Models/ApplicationState.cs`

**Serialization**: System.Text.Json (default in .NET 8)

**Evolution**: Future workflows will add properties for GNSS, Guidance, Section Control, etc.

## Architecture Abstractions

This workflow introduces transport abstraction layers to enable easy replacement of SignalR with other transports (WebSocket, gRPC, MQTT) in future.

### Backend Abstraction

**IStatePublisher** - Interface for broadcasting state to clients:

```csharp
public interface IStatePublisher
{
    Task BroadcastStateAsync(ApplicationState state, CancellationToken ct = default);
}
```

**Implementation**: `SignalRStatePublisher` (this workflow)

**Future**: `WebSocketStatePublisher`, `GrpcStatePublisher`, `MqttStatePublisher`

**Location**: `AgOpenGPS.Api/Abstractions/IStatePublisher.cs`

### Client Abstraction

**IStateSubscriber** - Interface for receiving state updates:

```csharp
public interface IStateSubscriber
{
    Task ConnectAsync(string url);
    Task DisconnectAsync();
    event EventHandler<ApplicationState> StateReceived;
    bool IsConnected { get; }
}
```

**Implementation**: `SignalRStateSubscriber` (this workflow)

**Future**: `WebSocketStateSubscriber`, `GrpcStateSubscriber`

**Location**: `AgOpenGPS.Api.Client/Abstractions/IStateSubscriber.cs`

### Transport Swapping

To replace SignalR with another transport:

1. Create new implementation (e.g., `GrpcStatePublisher : IStatePublisher`)
2. Change DI registration: `services.AddSingleton<IStatePublisher, GrpcStatePublisher>()`
3. No changes needed in ApplicationOrchestrator or FormGPS

## Architecture Pattern

**Backend-Driven with Publisher-Subscriber**

```
Backend (AgOpenGPS.Api - .NET 8)
  └─ ApplicationOrchestrator (10 Hz timer)
     └─ IStatePublisher (abstraction)
        └─ SignalRStatePublisher (implementation)
           └─ StateHub (SignalR detail)
              └─ HTTP/WebSockets
                 └─ FormGPS (WinForms - .NET Framework 4.8)
                    └─ IStateSubscriber (abstraction)
                       └─ SignalRStateSubscriber (implementation)
                          └─ StateReceived event → UI update
```

**Key Principles:**
- Backend owns timing and orchestration
- Frontend is passive receiver
- **Transport abstraction**: SignalR is just one implementation
- Strongly-typed state model (ApplicationState)
- HTTP/WebSockets transport (cross-platform, efficient)
- Two separate processes (easy to run, debug, and replace)
- **Easy transport swapping**: Replace SignalR with WebSocket/gRPC by changing DI registration

## Success Criteria

- [ ] AgOpenGPS.Api project created and builds successfully
- [ ] AgOpenGPS.Api.Client project created and builds successfully
- [ ] ApplicationState class created with Timestamp property
- [ ] IStatePublisher interface created (backend abstraction)
- [ ] SignalRStatePublisher implementation created
- [ ] IStateSubscriber interface created (client abstraction)
- [ ] SignalRStateSubscriber implementation created
- [ ] ApplicationOrchestrator runs at 10 Hz using IStatePublisher
- [ ] FormGPS connects using IStateSubscriber and receives state updates
- [ ] Legacy timers (tmrWatchdog, timerSim) disabled in FormGPS
- [ ] State update payload contains Timestamp (strongly-typed)
- [ ] Both processes can run simultaneously (Api.exe + FormGPS.exe)
- [ ] Manual testing confirms state updates flowing end-to-end
- [ ] Documentation updated (CLAUDE.md, architecture docs with abstractions)
