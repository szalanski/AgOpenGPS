# Task 5: Add Feature Flag

## Goal

Add UseBackendTick flag to choose between WinForms timer and backend timer.

## Steps

1. Add config: `UseBackendTick = false` (App.config or appsettings.json)
2. Load flag in FormGPS_Load
3. If false: `tmrWatchdog.Enabled = true`, don't init SignalR
4. If true: `tmrWatchdog.Enabled = false`, init SignalR
5. Add fallback: if SignalR fails, enable tmrWatchdog

## Key Points

- Default: false (legacy mode)
- Easy toggle: change config, restart
- Instant rollback if issues

## Acceptance

- [ ] Flag = false: legacy timer works
- [ ] Flag = true: backend timer works
- [ ] Both modes work identically
- [ ] Logged which mode active

## Test

1. Test with flag = false (legacy)
2. Test with flag = true (backend)
3. Compare behavior (should be identical)
