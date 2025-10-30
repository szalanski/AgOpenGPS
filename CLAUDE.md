# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AgOpenGPS is a precision agriculture mapping and section control application. The system consists of two main programs:
- **AgOpenGPS** - Main guidance application with GPS-based field mapping, AB line guidance, section control, and auto-steering
- **AgIO** - Communication hub that interfaces with external hardware (GPS, IMU, steering controllers, section controllers)

Additional utilities: AgDiag (diagnostics), ModSim (simulator), GPS_Out (GPS output), Keypad (external keypad support)

## Building and Testing

### Build Commands
```bash
# Build entire solution (from SourceCode directory or root)
dotnet build SourceCode/AgOpenGPS.sln

# Build in Release mode
dotnet build SourceCode/AgOpenGPS.sln -c Release

# Publish all applications to a single output folder
dotnet publish SourceCode/AgOpenGPS.sln
```

### Running Tests
```bash
# Run all tests
dotnet test SourceCode/AgOpenGPS.sln

# Run tests for specific project
dotnet test SourceCode/AgLibrary.Tests/AgLibrary.Tests.csproj
dotnet test SourceCode/AgOpenGPS.Core.Tests/AgOpenGPS.Core.Tests.csproj

# Run tests with detailed output
dotnet test SourceCode/AgOpenGPS.sln --verbosity detailed
```

Test framework: NUnit 4.x

### Running Applications
The main application executables are in the GPS and AgIO projects. After building, run from their respective bin directories, or use `dotnet run`:
```bash
dotnet run --project SourceCode/GPS/AgOpenGPS.csproj
dotnet run --project SourceCode/AgIO/Source/AgIO.csproj
```

## Running the Application (Backend-Driven Mode)

The system runs as two cooperating processes: the backend owns GNSS processing and publishes state, while FormGPS renders the streamed results.

### 1. Start Backend (required)
```bash
dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj
```
- Listens on http://localhost:5000 by default.
- Logs `ApplicationOrchestrator starting - GPS-driven UDP mode`.
- Waits for UDP packets on the configured port (15556 in default appsettings).

### 2. Start Frontend
```bash
dotnet run --project SourceCode/GPS/AgOpenGPS.csproj
```
- Connects to the backend via SignalR automatically.
- Logs `Backend connection established`.
- `tmrWatchdog` still fires every 250 ms but now handles UI housekeeping (status labels, layout); GNSS data is no longer computed on that tick.

### 3. Verify Data Flow
- Backend logs packet validation and state broadcasts whenever GNSS data arrives.
- FormGPS logs each `ReceiveState` callback. Update cadence matches the incoming UDP frequency (simulator and typical receivers run near 10 Hz).

**Reminder**: Start the backend first. If it is offline, FormGPS continues to render the last known values and surfaces connection errors until packets resume.

### Current Architecture Highlights
- **ApplicationOrchestrator** processes packets on arrival (no internal timer).
- **SimulatorHostedService** still ticks at 93 ms but writes PGN 0xD6 packets into UDP so the full pipeline is exercised.
- **ApplicationState.Gnss** is the canonical source for GPS, quality, and health metrics; FormGPS simply renders it.

## Architecture

### Current Platform Dependencies
- **Target Framework**: .NET Framework 4.8 (Windows-only)
- **UI Framework**: WinForms (GPS, AgIO, utilities) with some WPF migration in progress (AgOpenGPS.WpfApp, AgOpenGPS.WpfViews)
- **3D Rendering**: OpenTK 3.3.3 (OpenGL bindings)
- **Configuration Storage**: Windows Registry (via `RegistrySettings.cs`)

## IMPORTANT: Parallel Migration Initiatives

This codebase has TWO INDEPENDENT cross-platform initiatives:

### Initiative 1: AgOpenGPS.Core (MVP Pattern - Separate Team)
- **Project**: `AgOpenGPS.Core/`, `AgOpenGPS.WpfApp/`, `AgOpenGPS.WpfViews/`
- **Approach**: Model-View-Presenter pattern, WPF migration
- **Team**: Separate team (not this migration)
- **Status**: In progress by others

### Initiative 2: Backend API Migration (Strangler Fig - THIS Migration)
- **New Projects**: `AgOpenGPS.Api/` (.NET 8), `AgOpenGPS.Api.Client/` (.NET Standard 2.0)
- **Approach**: Backend-driven with SignalR, Strangler Fig Pattern
- **Documentation**: [docs/architecture/](docs/architecture/) and [docs/workflow/](docs/workflow/)
- **Team**: THIS migration work (cross-platform-support branch)
- **Status**: Planning phase

**These initiatives are COMPLETELY INDEPENDENT and do not share code.**

### Project Structure

**Core Libraries:**
- `AgLibrary/` - Shared utilities (logging, settings, UI controls)
- `AgOpenGPS.Core/` - **[SEPARATE INITIATIVE]** MVP pattern refactor (see Initiative 1 above)

**Main Applications:**
- `GPS/` - Main AgOpenGPS WinForms application
  - `GPS/Classes/` - Domain classes (CABLine, CABCurve, CBoundary, CSection, CTool, CVehicle, CGuidance, etc.)
  - `GPS/Forms/` - WinForms UI (FormGPS is the main form)
  - `GPS/Properties/RegistrySettings.cs` - Windows Registry-based configuration
- `AgIO/Source/` - Communication hub (WinForms)
- `AgOpenGPS.WpfApp/` - WPF application entry point (migration in progress)
- `AgOpenGPS.WpfViews/` - WPF views library (migration in progress)

**Utilities:**
- `AgDiag/` - Diagnostics tool
- `ModSim/Source/` - Module simulator
- `GPS_Out/Source/` - GPS output utility
- `Keypad/` - External keypad support

**Tests:**
- `AgLibrary.Tests/`
- `AgOpenGPS.Core.Tests/`
- `Tests/AgOpenGPS.API.IntegrationTests/` - Backend integration tests (NUnit)

### Backend API Migration (Strangler Fig Pattern - Initiative 2)

**Status**: Workflows 001, 002, 005, 006, 007 Completed - See [docs/README.md](docs/README.md) and [docs/workflow/](docs/workflow/)
- ✅ Workflow 001: Backend State Foundation (ApplicationOrchestrator, SignalR)
- ✅ Workflow 002: GPS/GNSS Migration (GnssService, UdpPacketReceiver, Simulator)
- ✅ Workflow 005: Remove CNMEA Adapter (Direct ApplicationState access)
- ✅ Workflow 006: Coordinate Service Separation (ICoordinateService, origin synchronization)
- ✅ Workflow 007: Expose Steering Angle (ControlState domain, vehicle shake fix)

**New Projects**:
- `AgOpenGPS.Api/` (.NET 8) - Backend Web API
  - `Abstractions/IStatePublisher.cs` - Transport abstraction (backend)
  - `Abstractions/IGnssService.cs` - GPS processing service interface
  - `Abstractions/IUdpPacketReceiver.cs` - UDP packet reception interface
  - `Services/ApplicationOrchestrator.cs` - Event-driven main loop (processes UDP packets immediately)
  - `Services/SignalRStatePublisher.cs` - SignalR implementation
  - `Services/GnssService.cs` - GPS packet processing (PGN 0xD6 unpacking, coordinate transforms)
  - `Services/UdpPacketReceiver.cs` - UDP listener (port 15556)
  - `Services/SimulatorService.cs` - GPS simulator with physics (93ms tick rate, DDD refactored with DI)
  - `Services/VehiclePhysicsService.cs` - Vehicle physics calculations (speed transitions, steering smoothing, heading changes, position calculations)
  - `Services/GnssDataGenerator.cs` - GNSS data generation (altitude, satellites, fix quality, HDOP, age)
  - `Services/AgIoProtocolSerializer.cs` - Binary protocol encoding (PGN 0xD6 packet serialization)
  - `Services/SimulatorHostedService.cs` - Background service (sends UDP packets)
  - `Commands/Handlers/*CommandHandler.cs` - CQRS command handlers (Start/Stop/SetSpeed/SetSteering/Reset)
  - `Hubs/StateHub.cs` - SignalR Hub with specific command methods (workaround for SignalR generic limitation)
  - `Configuration/UdpOptions.cs` - UDP port configuration
  - `Models/UdpPacket.cs`, `GnssState.cs` - Domain models

- `AgOpenGPS.Api.Client/` (.NET Standard 2.0) - Client library for FormGPS
  - `Models/ApplicationState.cs` - Strongly-typed state (Timestamp, Gnss properties)
  - `Models/GnssState.cs` - GPS data (position, heading, speed, altitude, quality, health)
  - `Models/Wgs84Position.cs`, `LocalPosition.cs`, `Heading.cs`, `Speed.cs`, `Altitude.cs` - Coordinate value types (with C# 9.0 init setters)
  - `Models/ConnectionOptions.cs` - Backend connection configuration
  - `Commands/ICommand.cs` - CQRS marker interface
  - `Commands/SimulatorCommands.cs` - 5 command records (Start/Stop/SetSpeed/SetSteering/Reset)
  - `Abstractions/IBackendClient.cs` - Bidirectional communication interface (renamed from IStateSubscriber)
  - `SignalR/SignalRBackendClient.cs` - SignalR implementation with command routing
  - `Factories/BackendClientFactory.cs` - Factory for creating clients (renamed from SubscriberFactory)

**Tests**:
- `Tests/AgOpenGPS.API.IntegrationTests/` - Integration tests (41/44 passing)
  - `Common/BaseIntegrationTest.cs` - Base class for tests
  - `Common/TestWebApplicationFactory.cs` - In-memory test server
  - `Helpers/GpsSimulator.cs` - External GPS simulator helper for tests
  - `GpsPacketProcessingTests.cs` - GPS packet processing (7 tests, all passing)
  - `StateReceptionTests.cs` - State reception via SignalR (3 tests, all passing)
  - `SimulatorIntegrationTests.cs` - Backend simulator via CQRS commands (41 tests, 38 passing)
  - **Note**: Unit tests removed (low utility - all simulator behavior covered by integration tests)
  - **Known Issues**: 3 steering-related test failures (heading change too weak - to be fixed)

**Key Architecture Patterns**:
1. **Event-Driven Backend**: ApplicationOrchestrator processes UDP packets immediately (not timer-based) - ✅ IMPLEMENTED
2. **SignalR Bidirectional**: Backend pushes state, Client sends commands - ✅ IMPLEMENTED
3. **CQRS with MediatR**: Commands (Start/Stop/SetSpeed/SetSteering/Reset) dispatched via MediatR handlers - ✅ IMPLEMENTED
4. **SignalR Limitation Workaround**: Specific hub methods per command type (SignalR doesn't support generic hub methods) - ✅ IMPLEMENTED
5. **Transport Abstraction**: IStatePublisher/IBackendClient interfaces (easy to swap SignalR for WebSocket/gRPC)
6. **Factory Pattern**: BackendClientFactory creates configured clients
7. **Simulator UDP Communication**: SimulatorHostedService sends via UDP (not direct GnssService calls)
8. **Strangler Fig**: Gradually migrate GPS/Classes/ → AgOpenGPS.Api/Services/ - ✅ GPS/GNSS MIGRATED
9. **Keep running**: GPS application works throughout entire migration
10. **Future-ready**: Enable Electron + React frontend (Phase 2)

**Domain Modules to Migrate** (from GPS/Classes/):
- **Navigation & Path Planning**: CGuidance, CABLine, CABCurve, CYouTurn, CDubins, CHead, CTurn
- **Field & Geometry**: CBoundary, CFieldData, CWorldGrid, CFence, CFlag
- **Section Control**: CSection, CTool, CFeatureSettings
- **Vehicle & Hardware**: CVehicle, CAutoSteer, CAHRS, CNMEA, CModuleComm
- **Visualization**: CCamera, CGLM (OpenGL rendering data)
- **Simulation**: CSim

**Backend Service Interfaces** (in AgOpenGPS.Api):
- ✅ `IGnssService` - GPS processing (IMPLEMENTED - Workflow 002)
- ✅ `IUdpPacketReceiver` - UDP packet reception (IMPLEMENTED - Workflow 002)
- ✅ `IStatePublisher` - State broadcasting abstraction (IMPLEMENTED - Workflow 001)
- ✅ `ISimulationService` - Simulator (IMPLEMENTED via SimulatorService with DDD pattern: VehiclePhysicsService, GnssDataGenerator, AgIoProtocolSerializer)
- ✅ `ICoordinateService` - Coordinate transformations (IMPLEMENTED - Workflow 006: WGS84 ↔ Local Plane conversions, origin management, LocalPlaneInfo exposure)
- `IGuidanceService`, `IPathPlanner`, `ITramlineService` (Future)
- `IFieldService`, `IBoundaryService`, `IHeadlandGenerator` (Future)
- `ISectionControlService`, `ICoverageMapService` (Future)
- `IVehicleService`, `IImuService`, `IAutoSteerService` (Future)
- `IConfigurationService` (Future)

**Documentation Structure**:

See [docs/README.md](docs/README.md) for complete documentation navigation.

**Set A (Architecture)** - Static knowledge base (~100-200 lines each):
- [docs/architecture/01-goals.md](docs/architecture/01-goals.md) - Phase 1 goals and current problems
- [docs/architecture/02-strangler-fig.md](docs/architecture/02-strangler-fig.md) - Gradual migration pattern
- [docs/architecture/03-backend-driven.md](docs/architecture/03-backend-driven.md) - Backend-driven architecture
- [docs/architecture/04-signalr.md](docs/architecture/04-signalr.md) - Real-time communication
- [docs/architecture/05-adapter-pattern.md](docs/architecture/05-adapter-pattern.md) - Safe migration with feature flags

**Set B (Workflow)** - Task-based workflow (vertical slices):
- [docs/workflow/001-application-orchestrator/](docs/workflow/001-application-orchestrator/) - Backend main loop
  - plan.md - Concept only (~200 lines, zero code)
  - task1.md through task6.md - Independent units of work

**AI Workflow Instructions**:

When working on migration tasks:
1. **For context**: Read relevant architecture docs (Set A) - small files optimized for AI context
2. **For implementation**: Use workflow chunks (Set B) - each task is self-contained and actionable
3. **Don't read everything**: Tasks include necessary context or link to architecture docs
4. **Start here**: [docs/README.md](docs/README.md) for navigation and quick start guide

### AgOpenGPS.Core (MVP Pattern - Initiative 1)

**Status**: In progress by separate team (NOT part of our docs/architecture/ or docs/workflow/)

The `AgOpenGPS.Core` project follows Model-View-Presenter pattern:
- **Models** (`Models/`) - Domain data and business logic
- **ViewModels** (`ViewModels/`) - Presentation state (e.g., `ApplicationViewModel`)
- **Presenters** (`Presenters/`) - Coordinates between models and views (e.g., `ApplicationPresenter`, `FieldStreamerPresenter`)
- **Interfaces** (`Interfaces/Presenters/`) - Contracts like `IPanelPresenter`, `IErrorPresenter`

The `ApplicationCore` class serves as the composition root, wiring up models, view models, and presenters.

**Note**: This is a SEPARATE cross-platform approach using WPF. OUR migration (see docs/architecture/ and docs/workflow/) uses AgOpenGPS.Api with SignalR and Strangler Fig Pattern instead.

## Important Implementation Notes

### Domain Classes Location
Core domain logic resides in `SourceCode/GPS/Classes/`. Main classes include:
- **Guidance**: `CGuidance`, `CABLine`, `CABCurve`, `CContour`, `CRecordedPath`
- **Field**: `CBoundary`, `CBoundaryList`, `CFieldData`, `CWorldGrid`
- **Vehicle**: `CVehicle`, `CAutoSteer`, `CAHRS`, `CNMEA`
- **Sections**: `CSection`, `CTool`, `CFeatureSettings`
- **Path Planning**: `CDubins`, `CYouTurn`, `CHead`, `CHeadLine`, `CTurn`
- **Graphics**: `CCamera`, `CGLM` (OpenGL math utilities)

### Configuration Management
Currently uses Windows Registry (`SourceCode/GPS/Properties/RegistrySettings.cs`). Migration plan calls for file-based storage (JSON/XML) in a cross-platform location.

### Main Form
`SourceCode/GPS/Forms/FormGPS.cs` is the primary UI orchestrator. It integrates:
- OpenTK OpenGL rendering
- Native Windows API calls (User32.dll)
- Direct coupling to domain classes

**Migration note**: FormGPS logic needs to be separated into presentation adapters and domain services to support headless backend operation.

### Translation Support
Uses Weblate for localization. Resource files in `Translations/` folders (e.g., `AgOpenGPS.Core/Translations/gStr.resx`).

## Contributing Workflow

**Branch Strategy:**
- `master` - Stable releases
- `develop` - Active development (target branch for PRs)

**Contribution Steps:**
1. Checkout `develop` branch
2. Create feature branch from `develop`
3. Implement changes and commit
4. Create PR targeting `develop`

**Note**: This repository is on the `cross-platform-support` branch, which is focused on the Phase 1 cross-platform migration work.

## Common Build Issues

- **Missing .NET Framework 4.8**: Install .NET Framework 4.8 Developer Pack
- **OpenTK errors**: Ensure OpenTK 3.3.3 NuGet package is restored
- **Registry access errors**: Run with appropriate permissions or check `RegistrySettings.cs` error handling

## Additional Resources

- [Official Documentation](https://docs.agopengps.com/)
- [Community Forum](https://discourse.agopengps.com/)
- [PCB and Firmware Repository](https://github.com/agopengps-official/Boards)
- [Rate Control Repository](https://github.com/agopengps-official/Rate_Control)
