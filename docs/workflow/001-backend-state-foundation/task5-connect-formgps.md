# Task 5: Connect FormGPS to Backend and Replace Legacy Timers

## Goal

Connect FormGPS to the backend using IStateSubscriber abstraction, receive strongly-typed ApplicationState updates, and disable the legacy timers to complete the transition to backend-driven architecture.

## Steps

1. Add AgOpenGPS.Api.Client project reference to FormGPS project
2. Create IStateSubscriber field in FormGPS class
3. Instantiate SignalRStateSubscriber in FormGPS constructor or Load event
4. Add StateReceived event handler in FormGPS (receives ApplicationState)
5. Connect to backend on FormGPS startup (FormGPS_Load) using ConnectAsync
6. Add UI indicator to show state updates (label with Timestamp)
7. Disable legacy timers (tmrWatchdog, timerSim) - set Enabled = false
8. Ensure state updates trigger on UI thread (use Invoke/BeginInvoke)
9. Add disconnection handling on FormGPS close (DisconnectAsync)
10. Handle reconnection if backend disconnects (automatic in SignalRStateSubscriber)

## Key Points

- **Use IStateSubscriber abstraction** (not direct SignalR types)
- **SignalRStateSubscriber is the concrete implementation** for SignalR
- Backend URL: http://localhost:5000/statehub
- **Receive strongly-typed ApplicationState** (not dynamic/object)
- Use async/await for connection (ConnectAsync, DisconnectAsync)
- State updates arrive on background thread - marshal to UI thread with Invoke
- Legacy timers: Disable by setting Enabled = false
- Show Timestamp in UI to prove state updates flowing
- Reconnection is automatic (handled by SignalRStateSubscriber)
- Log state updates for debugging

## UI Changes

Add simple indicator to FormGPS:
- Label showing "Backend: Connected" or "Backend: Disconnected" (bind to IStateSubscriber.IsConnected)
- Label showing Timestamp from ApplicationState (e.g., "Last Update: 2025-10-24 14:23:45 UTC")
- Placement: Status bar or top of main form

Example:
```csharp
// In StateReceived event handler (marshaled to UI thread)
private void OnStateReceived(object sender, ApplicationState state)
{
    if (InvokeRequired)
    {
        BeginInvoke(new EventHandler<ApplicationState>(OnStateReceived), sender, state);
        return;
    }

    lblBackendTimestamp.Text = $"Backend: {state.Timestamp:HH:mm:ss.fff}";
    lblBackendStatus.Text = "Connected";
}
```

## Legacy Timer Migration

Timers to disable:
- tmrWatchdog (250ms timer in GUI.Designer.cs) - set Enabled = false
- timerSim (93ms timer in Controls.Designer.cs) - set Enabled = false

State updates will replace these timers. Logic migration comes in later workflows.

## Code Structure

### FormGPS class changes:

```csharp
public partial class FormGPS : Form
{
    private IStateSubscriber _stateSubscriber;

    private async void FormGPS_Load(object sender, EventArgs e)
    {
        // Create SignalR subscriber
        _stateSubscriber = new SignalRStateSubscriber();
        _stateSubscriber.StateReceived += OnStateReceived;

        // Connect to backend
        await _stateSubscriber.ConnectAsync("http://localhost:5000/statehub");

        // Disable legacy timers
        tmrWatchdog.Enabled = false;
        timerSim.Enabled = false;
    }

    private void OnStateReceived(object sender, ApplicationState state)
    {
        // Marshal to UI thread and update UI
    }

    private async void FormGPS_FormClosing(object sender, FormClosingEventArgs e)
    {
        await _stateSubscriber?.DisconnectAsync();
    }
}
```

## Connection Lifecycle

1. FormGPS starts → Create SignalRStateSubscriber, subscribe to StateReceived event
2. FormGPS_Load → ConnectAsync to backend
3. Receive state updates → OnStateReceived handler updates UI (Timestamp)
4. Backend disconnects → SignalRStateSubscriber auto-reconnects
5. FormGPS closes → DisconnectAsync from backend gracefully

## Acceptance

- [ ] AgOpenGPS.Api.Client referenced in FormGPS project
- [ ] IStateSubscriber field added to FormGPS class
- [ ] SignalRStateSubscriber instance created in FormGPS
- [ ] StateReceived event handler implemented (receives ApplicationState)
- [ ] Connection established on FormGPS startup (ConnectAsync)
- [ ] UI shows Timestamp from ApplicationState updates
- [ ] UI shows connection status (Connected/Disconnected)
- [ ] Legacy timers disabled (tmrWatchdog.Enabled = false, timerSim.Enabled = false)
- [ ] State updates marshaled to UI thread (Invoke/BeginInvoke)
- [ ] Disconnection handled gracefully on FormGPS close (DisconnectAsync)
- [ ] No direct dependency on SignalR types in FormGPS (only IStateSubscriber)

## Test

Manual testing:
1. Start AgOpenGPS.Api (backend runs)
2. Start FormGPS (should connect automatically)
3. UI should show Timestamp updating every 100ms (e.g., "Backend: 14:23:45.123")
4. UI should show "Connected" status
5. Stop backend - FormGPS should show disconnected status
6. Restart backend - FormGPS should reconnect automatically (SignalRStateSubscriber handles this)
7. Close FormGPS - connection should disconnect gracefully
8. Verify legacy timers (tmrWatchdog, timerSim) are not executing (check logs)
