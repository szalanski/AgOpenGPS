# System Overview

## Two-Process Architecture

The AgOpenGPS backend system uses a **two-process architecture** with clear separation of concerns:

### Backend Process (AgOpenGPS.Api)
- **Platform**: .NET 8 (cross-platform)
- **Role**: Owns all business logic, timing, and state management
- **Location**: [SourceCode/AgOpenGPS.Api/](../../SourceCode/AgOpenGPS.Api/)
- **Entry point**: `dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj`
- **Network**: Runs on `http://localhost:5000`
- **Responsibility**:
  - Receives GPS data from external sources
  - Processes and transforms GPS data
  - Broadcasts application state to connected clients
  - Responds to commands from clients
  - Runs built-in simulator when needed

### Frontend Process (FormGPS)
- **Platform**: .NET Framework 4.8 (Windows-only, legacy)
- **Role**: Displays UI and sends user commands
- **Location**: [SourceCode/GPS/](../../SourceCode/GPS/)
- **Entry point**: `dotnet run --project SourceCode/GPS/AgOpenGPS.csproj`
- **Responsibility**:
  - Connects to backend via SignalR
  - Subscribes to state updates
  - Renders UI (OpenGL visualization, forms, controls)
  - Sends user commands to backend
  - No business logic (thin client pattern)

## Why Two Processes?

The separation enables several key capabilities:

### Cross-Platform Backend
Backend runs on any platform (.NET 8 supports Windows, Linux, macOS), while frontend remains Windows-only during migration. In Phase 2, frontend will be replaced with web-based UI (Electron + React).

### Headless Operation
Backend can run independently without UI:
- **Testing**: Integration tests run backend without FormGPS
- **Server deployment**: Backend-only operation for centralized systems
- **Automation**: Scripted control via API without GUI

### Thin Client Pattern
Frontend has no business logic:
- **Simple UI**: Only rendering and user input
- **Easy replacement**: Swap WinForms for web UI without touching logic
- **Multiple clients**: Different UIs can connect to same backend

### Clear Ownership
Backend owns timing and state:
- **No frontend timers**: Backend drives application rate (event-driven)
- **Single source of truth**: All state computed in backend
- **Simplified frontend**: No duplicate logic or synchronization issues

## System Startup Sequence

The system starts in a specific order:

### 1. Backend Starts
```
dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj
```
- ASP.NET Core Web API initializes
- SignalR hub starts on `/statehub` endpoint
- ApplicationOrchestrator background service starts
- UDP listener opens port 15556 (ready for GPS data)
- Logs: **"ApplicationOrchestrator starting - GPS-driven UDP mode"**

### 2. Frontend Connects
```
dotnet run --project SourceCode/GPS/AgOpenGPS.csproj
```
- FormGPS launches
- Creates SignalR client via factory
- Connects to backend at `http://localhost:5000/statehub`
- Subscribes to state updates
- Backend logs: **"Client connected: {ConnectionId}"**
- FormGPS logs: **"Backend connection established"**

### 3. GPS Data Arrives
Two possible sources:

**Option A: External GPS (AgIO)**
- AgIO (separate process) sends UDP packets on port 15556
- Backend receives and processes packets
- ApplicationOrchestrator broadcasts GPS state
- FormGPS displays GPS data

**Option B: Built-in Simulator**
- User enables simulator (via FormGPS controls or commands)
- Backend simulator generates GPS data every 93ms
- Simulator sends UDP packets to itself (port 15556)
- Same processing path as external GPS

### 4. Normal Operation
- Backend continuously processes incoming GPS data (event-driven)
- Each GPS packet triggers state computation and broadcast
- FormGPS receives state updates via SignalR
- UI updates automatically with new state
- User commands flow from FormGPS → Backend → State changes

## System Shutdown Sequence

### Normal Shutdown
1. User closes FormGPS
2. SignalR connection closes gracefully
3. Backend logs: **"Client disconnected: {ConnectionId}"**
4. Backend continues running (can serve other clients)
5. User stops backend (Ctrl+C)
6. ApplicationOrchestrator stops cleanly
7. UDP listener closes

### Abnormal Shutdown
If FormGPS crashes or connection lost:
- Backend detects disconnection
- Logs error if exception occurred
- Continues running (resilient to client failures)
- FormGPS can reconnect on restart

## Component Diagram

```
┌─────────────────────────────────────────────────────────┐
│  Backend Process (AgOpenGPS.Api)                        │
│  .NET 8 - Cross-platform                                │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │ ApplicationOrchestrator                        │    │
│  │ (Event-driven main loop)                       │    │
│  │                                                 │    │
│  │  ┌───────────────┐      ┌──────────────┐      │    │
│  │  │ UDP Receiver  │─────▶│ GNSS Service │      │    │
│  │  │ (Port 15556)  │      │ (GPS Process)│      │    │
│  │  └───────────────┘      └──────────────┘      │    │
│  │          │                       │             │    │
│  │          └───────────┬───────────┘             │    │
│  │                      ▼                         │    │
│  │             ┌──────────────────┐               │    │
│  │             │  State Publisher │               │    │
│  │             │  (SignalR Hub)   │               │    │
│  │             └──────────────────┘               │    │
│  └─────────────────────│───────────────────────────┘    │
│                        │                                │
│  ┌─────────────────────┴───────────────────────────┐    │
│  │ Simulator (Optional)                            │    │
│  │ - Vehicle Physics                               │    │
│  │ - GNSS Data Generation                          │    │
│  │ - Sends UDP packets (mimics AgIO)               │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
                         │ SignalR (TCP)
                         │ State updates (GPS-driven rate)
                         │ Commands (on-demand)
                         ▼
┌─────────────────────────────────────────────────────────┐
│  Frontend Process (FormGPS)                             │
│  .NET Framework 4.8 - Windows-only                      │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │ SignalR Client                                  │    │
│  │ - Subscribes to state updates                   │    │
│  │ - Sends commands                                │    │
│  └────────────────────────────────────────────────┘    │
│                         │                               │
│                         ▼                               │
│  ┌────────────────────────────────────────────────┐    │
│  │ UI Layer (WinForms + OpenGL)                    │    │
│  │ - Field visualization                           │    │
│  │ - Control panels                                │    │
│  │ - Settings                                      │    │
│  └────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘

External: AgIO (sends GPS) ────UDP Port 15556────▶ Backend
```

## Communication Flow

### State Updates (Backend → Frontend)
1. GPS data arrives via UDP (from AgIO or simulator)
2. Backend processes packet (coordinate transforms, calculations)
3. Backend computes application state
4. Backend broadcasts state via SignalR to all connected clients
5. FormGPS receives state update
6. FormGPS renders new state in UI

**Rate**: Event-driven (varies with GPS arrival rate, typically 5-10 Hz)

### Commands (Frontend → Backend)
1. User interacts with FormGPS (clicks button, adjusts setting)
2. FormGPS creates command object
3. FormGPS sends command via SignalR
4. Backend receives command
5. Backend processes command (updates simulator, changes settings)
6. State change propagates back via normal state update flow

**Rate**: On-demand (only when user acts)

## Key Characteristics

### Event-Driven Timing
Backend responds to external events (GPS data arrival), not internal timers. This matches real-world GPS behavior where data arrives at variable rates.

### Backend Ownership
Backend is authoritative:
- **Timing**: Backend decides when to process and broadcast
- **State**: Backend computes all state (frontend just displays)
- **Logic**: All business rules in backend

### Resilient Design
System handles failures gracefully:
- Frontend can disconnect/reconnect
- Backend continues running independently
- Multiple frontends can connect simultaneously
- UDP packet loss handled (GPS quality metrics)

### Migration-Friendly
Two-process design enables gradual migration:
- Backend extracted piece by piece (Strangler Fig pattern)
- Frontend continues working throughout migration
- Clear interface (SignalR) prevents tight coupling

## Current Status (Post Workflow 003)

### Fully Implemented
- ✅ Two-process architecture working
- ✅ Backend starts and runs independently
- ✅ SignalR bidirectional communication established
- ✅ GPS data pipeline complete (UDP → Processing → Broadcast)
- ✅ Built-in simulator with vehicle physics
- ✅ Command handling (CQRS pattern)
- ✅ Integration tests (41/44 passing)

### Partially Implemented
- ⚠️ FormGPS integration (connects but not fully migrated)
  - Backend connection working
  - State subscription working
  - UI still uses some legacy processing

### Not Yet Implemented
- ❌ Full FormGPS migration (still has legacy UDP processing)
- ❌ External AgIO integration testing
- ❌ Multiple client support (designed for, not tested)
- ❌ Web-based UI (Phase 2)

## Next Steps

The system foundation is complete. Future work focuses on:
1. Complete FormGPS migration (remove legacy UDP processing)
2. Migrate remaining subsystems (Guidance, Section Control, Boundaries)
3. Add more domain logic to backend
4. Eventually replace FormGPS with web UI (Phase 2)

## Related Documentation

- **[02-main-processing-loop.md](02-main-processing-loop.md)** - How backend processes data
- **[03-real-time-communication.md](03-real-time-communication.md)** - Communication details
- **[08-client-integration.md](08-client-integration.md)** - How to connect clients
- **[adr/001-event-driven-architecture.md](adr/001-event-driven-architecture.md)** - Why event-driven
- **[adr/002-backend-driven-timing.md](adr/002-backend-driven-timing.md)** - Why backend owns timing
