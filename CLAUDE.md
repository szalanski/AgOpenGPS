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

The application now uses a **two-process architecture**:

### 1. Start Backend (Required)
```bash
dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj
```
- Backend runs on http://localhost:5000
- ApplicationOrchestrator starts automatically (4 Hz / 250ms)
- Logs: "ApplicationOrchestrator starting - 4 Hz tick loop"

### 2. Start Frontend
```bash
dotnet run --project SourceCode/GPS/AgOpenGPS.csproj
```
- FormGPS connects to backend automatically via SignalR
- Logs: "Backend connection established"
- Legacy timer (tmrWatchdog) is **completely deleted** - backend now drives application loop

### 3. Verify Connection
- Backend logs: "ApplicationOrchestrator starting - 4 Hz tick loop"
- FormGPS logs: "Backend connection established"
- FormGPS logs: "Backend state received: HH:mm:ss.fff" (every 250ms)

**Note**: Backend must be running before starting FormGPS. If backend is unavailable, FormGPS will log connection error and use legacy timerSim for simulator only.

### Architecture Changes from Workflow 001
- **tmrWatchdog timer**: Completely deleted (was 250ms / 4 Hz)
- **ProcessApplicationTick()**: Refactored timer logic (called on backend state updates)
- **timerSim**: Still active at 93ms (simulator independent)
- **Backend frequency**: 4 Hz (250ms) matches original tmrWatchdog timing
- **State updates**: SignalR broadcasts ApplicationState with Timestamp

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

**Status**: In Progress - Workflow 001 Completed (see [docs/README.md](docs/README.md))

**New Projects**:
- `AgOpenGPS.Api/` (.NET 8) - Backend Web API
  - `Abstractions/IStatePublisher.cs` - Transport abstraction (backend)
  - `Services/ApplicationOrchestrator.cs` - Main loop (4 Hz / 250ms)
  - `Services/SignalRStatePublisher.cs` - SignalR implementation
  - `Hubs/StateHub.cs` - SignalR Hub

- `AgOpenGPS.Api.Client/` (.NET Standard 2.0) - Client library for FormGPS
  - `Models/ApplicationState.cs` - Strongly-typed state (Timestamp property)
  - `Models/ConnectionOptions.cs` - Backend connection configuration
  - `Abstractions/IStateSubscriber.cs` - Transport abstraction (implements IDisposable/IAsyncDisposable)
  - `SignalR/SignalRStateSubscriber.cs` - SignalR implementation
  - `Factories/SubscriberFactory.cs` - Factory for creating subscribers

**Tests**:
- `Tests/AgOpenGPS.API.IntegrationTests/` - Integration tests for state broadcasting
  - `Common/BaseIntegrationTest.cs` - Base class for tests
  - `Common/TestWebApplicationFactory.cs` - In-memory test server
  - `StateReceptionTests.cs` - State reception validation tests

**Key Architecture Patterns**:
1. **Backend-driven**: ApplicationOrchestrator main loop (4 Hz / 250ms) - ✅ IMPLEMENTED
2. **SignalR**: Real-time communication (Backend pushes state → WinForms) - ✅ IMPLEMENTED
3. **Transport Abstraction**: IStatePublisher/IStateSubscriber interfaces (easy to swap SignalR for WebSocket/gRPC)
4. **Factory Pattern**: SubscriberFactory creates configured subscribers
5. **Strangler Fig**: Gradually migrate GPS/Classes/ → AgOpenGPS.Api/Services/ (future workflows)
6. **Adapter Pattern**: Wrap legacy code to delegate to new API (future workflows)
7. **Keep running**: GPS application works throughout entire migration
8. **Future-ready**: Enable Electron + React frontend (Phase 2)

**Domain Modules to Migrate** (from GPS/Classes/):
- **Navigation & Path Planning**: CGuidance, CABLine, CABCurve, CYouTurn, CDubins, CHead, CTurn
- **Field & Geometry**: CBoundary, CFieldData, CWorldGrid, CFence, CFlag
- **Section Control**: CSection, CTool, CFeatureSettings
- **Vehicle & Hardware**: CVehicle, CAutoSteer, CAHRS, CNMEA, CModuleComm
- **Visualization**: CCamera, CGLM (OpenGL rendering data)
- **Simulation**: CSim

**Backend Service Interfaces** (in AgOpenGPS.Api):
- `IGuidanceService`, `IPathPlanner`, `ITramlineService`
- `IFieldService`, `IBoundaryService`, `IHeadlandGenerator`
- `ISectionControlService`, `ICoverageMapService`
- `IVehicleService`, `IGnssService`, `IImuService`, `IAutoSteerService`
- `IConfigurationService`, `ISimulationService`

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
