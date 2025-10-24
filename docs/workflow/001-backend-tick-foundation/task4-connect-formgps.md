# Task 4: Connect FormGPS to Backend Tick

## Goal

FormGPS connects to backend and calls tmrWatchdog_tick() when "Tick" received.

## Steps

1. Add NuGet: `Microsoft.AspNetCore.SignalR.Client` to GPS project
2. Add fields to FormGPS.cs: `HubConnection _hubConnection`, `bool _useBackendTick`
3. Add method: `InitializeSignalRAsync()` - connects to localhost:5000/hubs/tick
4. Subscribe to "Tick" event: `_hubConnection.On("Tick", OnBackendTick)`
5. OnBackendTick: call `tmrWatchdog_tick(this, EventArgs.Empty)`
6. Call InitializeSignalRAsync in FormGPS_Load
7. Cleanup in FormGPS_FormClosing

## Key Points

- Both timers run for now (hybrid state)
- tmrWatchdog_tick called TWICE per cycle (temporary)
- Zero changes to tmrWatchdog_tick method
- Automatic reconnect enabled

## Acceptance

- [ ] FormGPS connects to backend
- [ ] Receives tick signal
- [ ] Application works normally
- [ ] Works without backend (legacy timer continues)

## Test

1. Run backend
2. Run FormGPS
3. Check logs: "SignalR connected to backend tick"
4. Verify app works
