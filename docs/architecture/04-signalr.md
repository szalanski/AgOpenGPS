# SignalR Communication

## Why SignalR

**Real-time bidirectional communication** between backend and frontend.

**Advantages:**
- ✅ Backend → Frontend: Push (no polling)
- ✅ Frontend → Backend: RPC calls
- ✅ .NET Framework 4.8 compatible (`Microsoft.AspNetCore.SignalR.Client`)
- ✅ Easy migration: In-process (Phase 1) → HTTP (Phase 2)
- ✅ Thread-safe UI updates (SynchronizationContext support)

## Communication Flows

### Backend → Frontend (State Updates)

```
Backend: ApplicationOrchestrator
  └─ Calculate state (every 100ms)
      └─ StateHub.Clients.All.SendAsync("StateUpdated", state)
          ↓ SignalR Push
Frontend: FormGPS
  └─ HubConnection.On("StateUpdated", OnStateUpdated)
      └─ Update UI (thread-safe)
```

**No polling** - backend pushes when ready.

### Frontend → Backend (User Actions)

```
Frontend: FormGPS
  └─ User clicks "Create AB Line"
      └─ HubConnection.InvokeAsync("CreateABLine", dto)
          ↓ SignalR RPC
Backend: StateHub
  └─ CreateABLine(dto) method
      └─ GuidanceService.CreateABLineAsync()
          └─ Next loop broadcasts new state
```

**Single connection** - no need for separate HTTP requests.

## Architecture

### Backend: StateHub

```csharp
// AgOpenGPS.Api/Hubs/StateHub.cs
public class StateHub : Hub
{
    private readonly IGuidanceService _guidanceService;

    // Client→Server methods (user actions)
    public async Task CreateABLine(CreateABLineDto dto)
    {
        await _guidanceService.CreateABLineAsync(dto.PointA, dto.PointB);
        // State update in next main loop tick
    }

    public async Task ToggleSection(int index)
    {
        await _sectionService.ToggleSectionAsync(index);
    }
}
```

**Note:** State broadcast happens in ApplicationOrchestrator, NOT in Hub methods.

### Frontend: SignalR Client

```csharp
// GPS/Forms/FormGPS.cs
public partial class FormGPS : Form
{
    private HubConnection _hubConnection;
    private readonly SynchronizationContext _uiContext;

    private void InitializeSignalR()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/hubs/state") // Phase 2: HTTP
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<ApplicationStateDto>("StateUpdated", OnStateUpdated);
    }

    private void OnStateUpdated(ApplicationStateDto state)
    {
        // Marshal to UI thread (CRITICAL for WinForms!)
        _uiContext.Post(_ =>
        {
            lblSpeed.Text = state.Vehicle.Speed.ToString();
            lblHeading.Text = state.Vehicle.Heading.ToString();
            oglMain.Invalidate();
        }, null);
    }

    private async void btnCreateABLine_Click(object sender, EventArgs e)
    {
        var dto = new CreateABLineDto { ... };
        await _hubConnection.InvokeAsync("CreateABLine", dto);
    }
}
```

## Thread Safety (WinForms)

**Problem:** SignalR callbacks execute on background thread, WinForms requires UI thread.

**Solution:** `SynchronizationContext`

```csharp
// Capture UI thread context in constructor
_uiContext = SynchronizationContext.Current;

// In SignalR callback
private void OnStateUpdated(ApplicationStateDto state)
{
    _uiContext.Post(_ =>
    {
        // Safe to update UI controls here
        lblSpeed.Text = state.Vehicle.Speed.ToString();
    }, null);
}
```

**Alternative:** `Control.Invoke()`
```csharp
if (InvokeRequired)
{
    Invoke(new Action(() => OnStateUpdated(state)));
    return;
}
// Safe to update UI here
```

## Phase 1 vs Phase 2

### Phase 1: In-Process SignalR

```csharp
// Both backend + frontend in same process
// No HTTP overhead, fast debugging

var backendProvider = BuildBackendServices();
var hubConnection = new InProcessSignalRConnection(backendProvider);
```

**Benefits:**
- Fast (no HTTP serialization)
- Easy debugging (single process)
- Simple setup

### Phase 2: HTTP SignalR

```csharp
// Backend runs as separate Web API server
// Frontend connects via HTTP

var hubConnection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hubs/state")
    .Build();
```

**Benefits:**
- True separation (backend as service)
- Multiple frontends can connect
- Ready for Electron

**Migration:** Just change connection URL!

## Data Transfer Objects

### ApplicationStateDto (complete state)

```csharp
public class ApplicationStateDto
{
    public VehicleStateDto Vehicle { get; set; }
    public GuidanceStateDto Guidance { get; set; }
    public SectionStateDto[] Sections { get; set; }
    public FieldStateDto Field { get; set; }
    public DateTime Timestamp { get; set; }
}
```

Broadcast every 100ms (10 Hz).

### User Action DTOs

```csharp
public class CreateABLineDto
{
    public Vector3Dto PointA { get; set; }
    public Vector3Dto PointB { get; set; }
}

public class ToggleSectionDto
{
    public int SectionIndex { get; set; }
}
```

## Setup (DI)

### Backend
```csharp
// Program.cs
builder.Services.AddSignalR();

app.MapHub<StateHub>("/hubs/state");
```

### Frontend
```csharp
// Install NuGet:
// Microsoft.AspNetCore.SignalR.Client (9.0.0)

// FormGPS constructor
InitializeSignalR();
await _hubConnection.StartAsync();
```

## Why NOT WebSocket/gRPC?

**WebSocket:**
- Lower level (need to implement RPC ourselves)
- No automatic reconnection
- No hub abstraction

**gRPC:**
- Not WinForms-friendly (.NET Framework issues)
- Bi-directional streaming more complex
- SignalR simpler for our use case

**SignalR:**
- ✅ Built on WebSocket (when available)
- ✅ Automatic reconnection
- ✅ Hub abstraction (RPC-like)
- ✅ .NET Framework compatible

## References

- See: 03-backend-driven.md (ApplicationOrchestrator concept)
- See: workflow/001-application-orchestrator/plan.md (implementation)
