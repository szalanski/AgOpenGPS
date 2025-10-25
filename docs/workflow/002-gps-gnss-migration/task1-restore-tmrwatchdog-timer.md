# Task 1: Restore tmrWatchdog Timer

## Goal

Revert workflow 001 changes to restore tmrWatchdog timer for UI-only updates and stop calling ProcessApplicationTick from backend state updates.

## Steps

1. Recreate tmrWatchdog timer control in FormGPS.Designer.cs with 250ms interval
2. Rename ProcessApplicationTick() back to tmrWatchdog_Tick(object sender, EventArgs e) in GUI.Designer.cs
3. Remove ProcessApplicationTick() call from OnStateReceived() in FormGPS.cs
4. Restore any timer-related code removed from OpenGL.Designer.cs during workflow 001
5. Rebuild GPS project and verify timer executes

## Key Points

- Timer runs independently of backend connection
- Timer interval: 250ms (4 Hz) - matches original frequency
- OnStateReceived() should only log and store state, not execute tick logic
- ProcessApplicationTick() method name reverted to tmrWatchdog_Tick event handler
- Backend connection stays active but doesn't drive UI updates anymore

## Acceptance

- [ ] tmrWatchdog timer recreated in FormGPS.Designer.cs
- [ ] Timer interval set to 250ms
- [ ] tmrWatchdog_Tick event handler exists in GUI.Designer.cs
- [ ] OnStateReceived() no longer calls ProcessApplicationTick()
- [ ] GPS project builds successfully
- [ ] Timer executes independently of backend state updates

## Test

Run FormGPS without backend - UI updates (labels, colors, button states) should still work via timer.
