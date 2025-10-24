# Task 1: Create ApplicationOrchestrator Class Skeleton

## Objective

Create basic ApplicationOrchestrator class that implements IHostedService with DI setup.

## Context

ApplicationOrchestrator is the main backend loop coordinator. This task creates the skeleton without timer logic.

See: [plan.md](plan.md) for full architecture.

## What to Create

### File Structure

```
AgOpenGPS.Api/
  └─ Orchestration/
      ├─ ApplicationOrchestrator.cs
      └─ OrchestrationConfig.cs
```

### ApplicationOrchestrator.cs

- Implements IHostedService, IDisposable
- Constructor with DI:
  - IHubContext<StateHub>
  - Service interfaces (IGnssService, IVehicleService, IGuidanceService, etc.)
  - ILogger<ApplicationOrchestrator>
  - OrchestrationConfig
- StartAsync() / StopAsync() methods (empty implementation for now)
- Dispose() method

### OrchestrationConfig.cs

Configuration class with:
- MainLoopFrequencyHz (default: 10)
- MainLoopIntervalMs (calculated: 1000 / frequency)
- FieldCoverageUpdateInterval (default: 10 ticks)
- SignalRBroadcastThrottleMs (default: 0)
- EnablePerformanceLogging (default: true)

### DI Registration (Program.cs)

- Load OrchestrationConfig from appsettings.json
- Register as singleton
- Register ApplicationOrchestrator as IHostedService

### appsettings.json

Add Orchestration section:
```json
{
  "Orchestration": {
    "MainLoopFrequencyHz": 10,
    "FieldCoverageUpdateInterval": 10,
    "SignalRBroadcastThrottleMs": 0,
    "EnablePerformanceLogging": true
  }
}
```

## Acceptance Criteria

- ✅ ApplicationOrchestrator class exists
- ✅ Implements IHostedService
- ✅ All service dependencies injected via constructor
- ✅ OrchestrationConfig loads from appsettings.json
- ✅ Registered as IHostedService in Program.cs
- ✅ Backend starts without errors
- ✅ Logs "ApplicationOrchestrator starting" on startup

## Testing

Run backend:
```bash
dotnet run --project AgOpenGPS.Api
```

Verify log output:
```
ApplicationOrchestrator starting. Main loop: 10 Hz (100 ms)
```

## Notes

- No timer logic yet (added in task2)
- No service calls yet (added in task3-5)
- Focus on DI setup and configuration
