# Establish Frontend-Backend Connection

## Goal

Fix the abstraction mismatch in FormGPS, establish working SignalR connection between the WinForms frontend and AgOpenGPS.Api backend, display GPS data from backend state in the UI, and implement simulator controls to enable full end-to-end testing.

## Current State

- AgOpenGPS.Api backend fully implemented (StateHub, ApplicationOrchestrator, SignalRStatePublisher)
- AgOpenGPS.Api.Client library exists with IBackendClient, BackendClientFactory, SignalRBackendClient
- FormGPS.cs has connection skeleton code but references outdated abstractions (IStateSubscriber, SubscriberFactory)
- Build/runtime failures due to missing type references
- No end-to-end connection established

## Target State

- FormGPS.cs uses correct abstractions (IBackendClient, BackendClientFactory)
- FormGPS connects successfully to backend on startup
- State updates flow from backend → frontend via SignalR
- UI displays GPS data from backend state (speed, fix quality, frequency)
- Simulator controls send CQRS commands to backend (start/stop, speed, steering, reset)
- Full end-to-end testing possible: backend simulator → state updates → UI display → user controls → backend commands
- Foundation complete for future business logic migration (GPS, guidance, sections)

## Why

- **Completes Workflow 001 foundation**: Proves backend-driven architecture works end-to-end
- **Unblocks future workflows**: GPS migration, vehicle state, guidance all depend on working connection
- **Validates architecture**: Confirms SignalR transport and state broadcasting work correctly
- **Demonstrates value**: Shows real-time updates flowing without frontend timers
- **Enables testing**: User can see GPS data and control simulator to verify system works
- **Proves bidirectional communication**: Backend → Frontend (state) and Frontend → Backend (commands) both functional

## What This Is NOT

- **NOT migrating business logic**: GPS parsing, guidance calculations, section control stay in FormGPS (later workflows)
- **NOT updating documentation**: Skipping task5 documentation updates per user request
- **NOT adding advanced features**: Connection health monitoring, retry policies, metrics (future enhancements)
- **NOT performance testing**: Basic functionality verification only (optimization later if needed)

## Migration Path

### Phase 1: Establish Full Connection (This Workflow)
1. Update FormGPS.cs to use IBackendClient and BackendClientFactory (Task 1)
2. Display GPS data from backend state in FormGPS UI (Task 2)
3. Wire simulator controls to send CQRS commands to backend (Task 3)
4. Verify project references manually (no separate task)
5. Test end-to-end functionality manually (no separate task)

### Phase 2: Future Enhancements (Later Workflows)
1. Add connection health monitoring
2. Implement retry policies and error recovery
3. Add performance metrics
4. Migrate business logic from FormGPS to backend services

## Tasks

1. [task1-fix-formgps-abstractions.md](task1-fix-formgps-abstractions.md) - Update FormGPS.cs to use IBackendClient and BackendClientFactory
2. [task2-display-gps-data.md](task2-display-gps-data.md) - Wire FormGPS UI labels to display GPS data from backend state
3. [task3-simulator-controls.md](task3-simulator-controls.md) - Wire simulator panel buttons to send CQRS commands to backend

**Manual Verification Steps** (no separate task files):

**Step A: Verify Project References**
- Check GPS.csproj has reference to AgOpenGPS.Api.Client project
- Add reference if missing
- Build both projects to verify no compilation errors

**Step B: End-to-End Connection Testing**
- Start backend: `dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj`
- Verify backend logs: "ApplicationOrchestrator starting - GPS-driven UDP mode"
- Start FormGPS: `dotnet run --project SourceCode/GPS/AgOpenGPS.csproj`
- Verify FormGPS logs: "Backend connection established"
- Verify UI shows timestamps updating (state flowing from backend)
- Stop backend, verify FormGPS detects disconnection
- Restart backend, verify FormGPS reconnects automatically

## Architecture Context

**Connection Flow**:
```
Backend (AgOpenGPS.Api)
  └─ ApplicationOrchestrator (event-driven, processes UDP packets)
     └─ IStatePublisher (abstraction)
        └─ SignalRStatePublisher (implementation)
           └─ StateHub (SignalR)
              └─ HTTP/WebSockets
                 └─ FormGPS (WinForms)
                    └─ IBackendClient (abstraction)
                       └─ SignalRBackendClient (implementation)
                          └─ OnStateReceived event → UI update
```

**Key Abstractions**:
- **IBackendClient**: Client-side transport abstraction (receive state, send commands)
- **BackendClientFactory**: Creates SignalRBackendClient instances with configuration
- **IStatePublisher**: Server-side transport abstraction (broadcast state to clients)
- **ApplicationState**: Strongly-typed state model (Timestamp, Gnss, etc.)

**Backend URL**: http://localhost:5000/statehub

## Success Criteria

**Task 1: Connection**
- [ ] FormGPS.cs updated to use IBackendClient (not IStateSubscriber)
- [ ] FormGPS.cs updated to use BackendClientFactory (not SubscriberFactory)
- [ ] Project builds successfully without type errors
- [ ] Backend starts and logs "ApplicationOrchestrator starting"
- [ ] FormGPS starts and logs "Backend connection established"
- [ ] Automatic reconnection works when backend restarts
- [ ] Graceful disconnection works when FormGPS closes
- [ ] Legacy timers remain disabled (tmrWatchdog, timerSim)

**Task 2: GPS Data Display**
- [ ] lblSpeed displays speed from backend GnssState
- [ ] lblFix displays fix quality and satellite count from backend GnssState
- [ ] lblHz displays GPS frequency from backend GnssState
- [ ] UI updates smoothly without flickering or cross-thread exceptions
- [ ] GPS data values match backend simulator output

**Task 3: Simulator Controls**
- [ ] Speed up/down buttons send SpeedAdjust commands to backend
- [ ] Speed zero button sends SpeedZero command to backend
- [ ] Steering scrollbar sends SteeringSet commands to backend
- [ ] Reset steering button sends SteeringReset command to backend
- [ ] Reverse direction button sends DirectionReverse command to backend
- [ ] Reset simulator button sends Reset command to backend
- [ ] Backend simulator responds to commands correctly (visible in GPS data changes)
- [ ] Command errors handled gracefully with user feedback
