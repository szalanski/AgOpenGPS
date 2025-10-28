# Task 3: Implement Simulator Control via CQRS Commands

## Goal

Wire existing simulator panel buttons in FormGPS to send CQRS commands to the backend, enabling user control of the backend simulator and demonstrating bidirectional communication (Frontend → Backend commands).

## Steps

1. Locate the simulator control panel (panelSim) in FormGPS
2. Identify existing button click handlers for simulator controls
3. Add IBackendClient.SendCommandAsync calls to each button handler
4. Wire btnSimSpeedUp to send UpdateSimulatorCommand with SpeedAdjust(+1.0) event
5. Wire btnSpeedDn to send UpdateSimulatorCommand with SpeedAdjust(-1.0) event
6. Wire btnSimSetSpeedToZero to send UpdateSimulatorCommand with SpeedZero event
7. Wire hsbarSteerAngle ValueChanged to send UpdateSimulatorCommand with SteeringSet event
8. Wire btnResetSteerAngle to send UpdateSimulatorCommand with SteeringReset event
9. Wire btnSimReverseDirection to send UpdateSimulatorCommand with DirectionReverse event
10. Wire btnResetSim to send UpdateSimulatorCommand with Reset event
11. Add error handling for command send failures (catch exceptions, log errors)
12. Add optional visual feedback for command acknowledgment (button state, status messages)
13. Test each control to verify commands reach backend and affect simulator state

## Key Points

- **Existing UI controls**: All buttons and scrollbar already exist in panelSim (no new controls needed)
- **Unified command**: All simulator events use single UpdateSimulatorCommand wrapper
- **Event types**: SpeedAdjust, SpeedSet, SpeedZero, SteeringSet, SteeringReset, DirectionReverse, Reset
- **SendCommandAsync**: Use IBackendClient.SendCommandAsync<TCommand>(command) method
- **Async handlers**: Button click handlers should be async to await SendCommandAsync
- **Error handling**: Log errors and optionally show MessageBox to user
- **No response validation**: Fire-and-forget commands (result visible in GPS data changes)
- **Value objects**: Commands use SteeringAngle, Speed value objects (construct from raw values)

## Simulator Panel Controls

**Panel**: panelSim (TableLayoutPanel at bottom-center of FormGPS)

**Controls to wire**:
- btnResetSim (Button) - Column 0
- btnResetSteerAngle (Button) - Column 1, text ">0<"
- hsbarSteerAngle (HScrollBar) - Column 3, range for steering angle
- btnSpeedDn (RepeatButton) - Column 5, down arrow image
- btnSimSetSpeedToZero (Button) - Column 6
- btnSimSpeedUp (RepeatButton) - Column 7, up arrow image
- btnSimReverseDirection (Button) - Column 8

## Command Structure

**UpdateSimulatorCommand**:
- Single wrapper command containing a SimulatorEvent
- SimulatorEvent is discriminated union of event types

**Event factory methods** (in SimulatorCommands.cs):
- SimulatorEvent.SpeedAdjust(double delta)
- SimulatorEvent.SpeedZero()
- SimulatorEvent.SteeringSet(SteeringAngle angle)
- SimulatorEvent.SteeringReset()
- SimulatorEvent.DirectionReverse()
- SimulatorEvent.Reset()

**Value objects**:
- Speed: Speed.FromKilometersPerHour(double kmh)
- SteeringAngle: SteeringAngle.FromDegrees(double degrees)

## Button Wiring Examples

**Speed controls**:
- btnSimSpeedUp: SpeedAdjust(+1.0) - increases speed by 1 km/h
- btnSpeedDn: SpeedAdjust(-1.0) - decreases speed by 1 km/h
- btnSimSetSpeedToZero: SpeedZero() - instant stop

**Steering controls**:
- hsbarSteerAngle: SteeringSet(angle from scrollbar value)
- btnResetSteerAngle: SteeringReset() - center steering to 0°

**Direction/reset controls**:
- btnSimReverseDirection: DirectionReverse() - flip heading 180°
- btnResetSim: Reset() - full simulator reset

## Error Handling Strategy

1. Wrap SendCommandAsync in try-catch block
2. Log exceptions to FormGPS logger
3. Optionally display MessageBox.Show with error message
4. Continue UI operation (don't crash on command failure)
5. Consider disabling controls if backend disconnected (check IBackendClient.IsConnected)

## Visual Feedback Options

**Basic** (minimal implementation):
- No explicit feedback, user sees GPS data change when command succeeds

**Enhanced** (optional):
- Change button color briefly on click (green flash)
- Show status message in status bar ("Command sent", "Command failed")
- Disable controls while backend disconnected
- Show loading indicator for slow commands

## Async Pattern for Button Handlers

Button click handlers should use async/await pattern:
1. Declare handler as async void (required for event handlers)
2. Await SendCommandAsync call
3. Catch and handle exceptions
4. Update UI based on result (success/failure feedback)

## Integration with Task 2

Once Task 2 (GPS data display) is complete:
- User sends command (Task 3)
- Backend simulator processes command
- Backend broadcasts updated state
- FormGPS displays updated GPS data (Task 2)
- User sees feedback loop: button click → speed/heading change in display

## Acceptance

- [ ] btnSimSpeedUp sends SpeedAdjust(+1.0) command to backend
- [ ] btnSpeedDn sends SpeedAdjust(-1.0) command to backend
- [ ] btnSimSetSpeedToZero sends SpeedZero command to backend
- [ ] hsbarSteerAngle sends SteeringSet command with scrollbar value
- [ ] btnResetSteerAngle sends SteeringReset command to backend
- [ ] btnSimReverseDirection sends DirectionReverse command to backend
- [ ] btnResetSim sends Reset command to backend
- [ ] All commands use IBackendClient.SendCommandAsync
- [ ] Button handlers are async and await command completion
- [ ] Exceptions are caught and logged without crashing UI
- [ ] Commands reach backend successfully (verified via backend logs)
- [ ] Backend simulator responds to commands (visible in GPS data changes)
- [ ] No compiler warnings or errors

## Test

Manual testing workflow:
1. Start backend with simulator: `dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj`
2. Start FormGPS: `dotnet run --project SourceCode/GPS/AgOpenGPS.csproj`
3. Verify GPS data displays (Task 2 prerequisite)
4. Click btnSimSpeedUp several times
5. Verify lblSpeed increases by ~1 km/h per click
6. Click btnSimSetSpeedToZero
7. Verify lblSpeed drops to 0
8. Move hsbarSteerAngle scrollbar
9. Verify heading changes in GPS data (if displayed)
10. Click btnResetSteerAngle
11. Verify heading returns to original direction
12. Click btnSimReverseDirection
13. Verify heading flips 180 degrees
14. Click btnResetSim
15. Verify simulator returns to initial state
16. Check backend logs for command receipt confirmation
17. Check FormGPS logs for no exceptions or errors
