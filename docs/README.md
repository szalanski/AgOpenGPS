# AgOpenGPS AI Context Guide

## Project Overview
**Mission**: Migrate AgOpenGPS from monolithic WinForms to backend-driven architecture (Phase 1), enabling future Electron+React frontend (Phase 2).

**Current State**: Workflows 001-007 completed. Backend processes GNSS via UDP, broadcasts state via SignalR. FormGPS remains functional but delegates GPS processing to backend.

## Core Architecture

### Migration Strategy: Strangler Fig Pattern
- **Gradual replacement** without breaking the app at any commit
- **Feature flags** toggle between legacy/backend implementations
- **Adapter pattern** wraps legacy code with clean interfaces
- **Safety first**: Application must work after every commit

### Backend-Driven Event Processing
```
UDP Packets (port 15556) → UdpPacketReceiver → ApplicationOrchestrator (event-driven)
→ GnssService → SignalRStatePublisher → FormGPS/Frontends
```

**Key Services**:
- `ApplicationOrchestrator`: Event-driven main loop, processes packets on arrival
- `GnssService`: GNSS processing, coordinate transforms (WGS84↔Local)
- `UdpPacketReceiver`: Validates AgIO protocol packets
- `SignalRStatePublisher`: Broadcasts `ApplicationState` to all clients
- `SimulatorHostedService`: Generates realistic test packets at 10Hz

### Communication: SignalR
- **Bidirectional**: Backend pushes state, frontend sends commands
- **CQRS Commands**: Start/Stop/SetSpeed/SetSteering/Reset (via MediatR)
- **Thread-safe**: Uses `SynchronizationContext.Post()` for WinForms UI updates
- **Transport abstraction**: Easy to swap SignalR for gRPC/WebSocket later

## Development Workflow

### Running the System
```bash
# 1. Start Backend (required first)
dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj
# Logs: "ApplicationOrchestrator starting - GPS-driven UDP mode"

# 2. Start Frontend
dotnet run --project SourceCode/GPS/AgOpenGPS.csproj
# Logs: "Backend connection established"
```

### Testing
```bash
dotnet test SourceCode/AgOpenGPS.sln
# 41 integration tests covering UDP→SignalR pipeline
```

## Essential Constraints

- **No breaking changes**: Every commit must compile and pass all tests
- **Event-driven architecture**: ApplicationOrchestrator processes UDP packets on arrival (no timers)
- **ApplicationState is canonical source**: Frontend renders streamed state, never computes GNSS locally
- **Thread-safe UI updates**: SignalR callbacks require `SynchronizationContext.Post()` marshaling
- **Feature flags for migration**: Adapter pattern with toggleable backend/legacy paths (appsettings.json)

## Documentation Structure

- `docs/architecture/`: Migration strategy and patterns (5 files)
- `docs/implementation/`: What's actually built (sections/, adrs/)
- `docs/workflow/`: Task-based implementation guides (001-008 completed/planned)
- `CLAUDE.md`: Full codebase context for AI agents

## Important Notes

### Independence from AgOpenGPS.Core
This migration (AgOpenGPS.Api) is **completely independent** from the AgOpenGPS.Core MVP/WPF initiative by another team. No code sharing, no conflicts.

## Planning New Workflows

When planning new work:
1. **Identify domain module** to migrate (e.g., "Guidance")
2. **Design DTOs** for API↔Client communication
3. **Create backend service** interface + implementation
4. **Add feature flag** in appsettings.json
5. **Implement adapter** in FormGPS with flag check
6. **Write integration tests** covering full pipeline
7. **Document rollback plan** and soak period

Each workflow = one vertical slice (complete feature migration).

---
*Context optimized for AI planning sessions. For detailed architecture, see subdirectories.*