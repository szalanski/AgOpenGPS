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

## Architecture

### Current Platform Dependencies
- **Target Framework**: .NET Framework 4.8 (Windows-only)
- **UI Framework**: WinForms (GPS, AgIO, utilities) with some WPF migration in progress (AgOpenGPS.WpfApp, AgOpenGPS.WpfViews)
- **3D Rendering**: OpenTK 3.3.3 (OpenGL bindings)
- **Configuration Storage**: Windows Registry (via `RegistrySettings.cs`)

### Project Structure

**Core Libraries:**
- `AgLibrary/` - Shared utilities (logging, settings, UI controls)
- `AgOpenGPS.Core/` - Domain models, interfaces, presenters, and view models for cross-platform refactor (uses MVP pattern)

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

### Cross-Platform Migration (Phase 1)

The codebase is undergoing architectural migration to support Linux/macOS backends. See [docs/migration/phase1-overview.md](docs/migration/phase1-overview.md) and [docs/migration/phase1-architecture.md](docs/migration/phase1-architecture.md) for detailed plans.

**Key Migration Goals:**
1. Decouple business logic from WinForms UI layer
2. Replace Windows Registry with cross-platform file-based configuration
3. Abstract OpenGL rendering behind platform-agnostic interfaces
4. Extract domain services following the module contracts defined in phase1-architecture.md

**Domain Module Organization (from phase1-architecture.md):**
- **Navigation & Path Planning**: CGuidance, CABLine, CABCurve, CYouTurn, CDubins, CHead, CTurn, etc.
- **Field & Geometry Management**: CBoundary, CFieldData, CWorldGrid, CFence, CFlag
- **Section Control & Implement**: CSection, CTool, CFeatureSettings
- **Vehicle & Hardware Integration**: CVehicle, CAutoSteer, CAHRS, CNMEA, CModuleComm
- **Visualization & 3D Graphics**: CCamera, CGLM, OpenTK rendering
- **Simulation & Testing**: CSim

**Planned Service Interfaces (from phase1-architecture.md):**
- `IGuidanceService`, `IPathPlanner`, `ITramlineService`
- `IFieldRepository`, `IBoundaryService`, `IHeadlandGenerator`
- `ISectionControlService`, `IImplementConfigurationProvider`, `ICoverageMapService`
- `IVehicleStateService`, `IAutoSteerGateway`, `IGnssGateway`, `IImuGateway`
- `IRenderSceneProvider`, `ICameraStateService`, `IThemeService`
- `ISimulationService`

### MVP Pattern in AgOpenGPS.Core
The `AgOpenGPS.Core` project follows Model-View-Presenter pattern:
- **Models** (`Models/`) - Domain data and business logic
- **ViewModels** (`ViewModels/`) - Presentation state (e.g., `ApplicationViewModel`)
- **Presenters** (`Presenters/`) - Coordinates between models and views (e.g., `ApplicationPresenter`, `FieldStreamerPresenter`)
- **Interfaces** (`Interfaces/Presenters/`) - Contracts like `IPanelPresenter`, `IErrorPresenter`

The `ApplicationCore` class serves as the composition root, wiring up models, view models, and presenters.

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
