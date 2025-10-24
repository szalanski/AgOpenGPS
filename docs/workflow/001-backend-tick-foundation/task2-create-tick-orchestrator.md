# Task 2: Create TickOrchestrator

## Goal

Create IHostedService with 250ms timer that broadcasts "Tick" via SignalR.

## Steps

1. Create folder: `SourceCode/AgOpenGPS.Api/Orchestration/`
2. Create `TickOrchestrator.cs`: IHostedService + IDisposable
3. Add Timer (250ms interval)
4. OnTick: broadcast via `IHubContext<TickHub>.Clients.All.SendAsync("Tick")`
5. Register in Program.cs: `AddHostedService<TickOrchestrator>()`

## Key Points

- 250ms interval (same as current tmrWatchdog)
- No payload, just "Tick" signal
- Overrun detection (skip if previous tick still running)
- Log every 100 ticks

## Acceptance

- [ ] TickOrchestrator starts with backend
- [ ] Logs show tick every 25 seconds
- [ ] No errors

## Test

Run backend, watch logs for "Tick #100, Interval: 250.Xms"
