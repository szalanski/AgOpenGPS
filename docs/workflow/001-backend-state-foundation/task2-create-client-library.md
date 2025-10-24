# Task 2: Create AgOpenGPS.Api.Client Project

## Goal

Create the AgOpenGPS.Api.Client library as a .NET Standard 2.0 class library with strongly-typed ApplicationState model, transport abstraction (IStateSubscriber), and SignalR implementation (SignalRStateSubscriber).

## Steps

1. Create new .NET Standard 2.0 class library in SourceCode/AgOpenGPS.Api.Client/
2. Add SignalR Client package dependency (Microsoft.AspNetCore.SignalR.Client)
3. Add System.Text.Json package dependency (for serialization)
4. Create folder structure: Models/, Abstractions/, SignalR/
5. Create ApplicationState class in Models/ (only Timestamp property)
6. Create IStateSubscriber interface in Abstractions/ (transport abstraction)
7. Create SignalRStateSubscriber class in SignalR/ (implements IStateSubscriber)
8. Implement connection management (ConnectAsync, DisconnectAsync)
9. Implement StateReceived event (strongly-typed with ApplicationState)
10. Configure automatic reconnection on disconnect
11. Verify project builds successfully

## Key Points

- Use .NET Standard 2.0 (compatible with .NET Framework 4.8 FormGPS)
- **ApplicationState is strongly-typed** (only Timestamp property for now)
- **IStateSubscriber is transport-agnostic** (enables future WebSocket/gRPC implementations)
- SignalRStateSubscriber wraps SignalR HubConnection
- Provide async/await connection methods
- Expose StateReceived event with ApplicationState type
- Handle reconnection automatically
- Use System.Text.Json for serialization (default in .NET 8, compatible with .NET Standard 2.0)

## Project Structure

After completion, the project should have:

```
AgOpenGPS.Api.Client/
├── Models/
│   └── ApplicationState.cs          // Strongly-typed state (Timestamp only)
├── Abstractions/
│   └── IStateSubscriber.cs          // Transport abstraction
└── SignalR/
    └── SignalRStateSubscriber.cs    // SignalR implementation
```

## API Design

### ApplicationState (Models/ApplicationState.cs)

```csharp
public class ApplicationState
{
    public DateTime Timestamp { get; set; }
}
```

### IStateSubscriber (Abstractions/IStateSubscriber.cs)

```csharp
public interface IStateSubscriber
{
    Task ConnectAsync(string url);
    Task DisconnectAsync();
    event EventHandler<ApplicationState> StateReceived;
    bool IsConnected { get; }
}
```

### SignalRStateSubscriber (SignalR/SignalRStateSubscriber.cs)

Implements IStateSubscriber using SignalR HubConnection:
- Wraps HubConnection for /statehub endpoint
- Handles ReceiveState SignalR method
- Fires StateReceived event on state updates
- Automatic reconnection on disconnect
- Connection state management

## Acceptance

- [ ] AgOpenGPS.Api.Client project exists in SourceCode/AgOpenGPS.Api.Client/
- [ ] Project targets .NET Standard 2.0
- [ ] SignalR Client packages added (Microsoft.AspNetCore.SignalR.Client)
- [ ] System.Text.Json package added
- [ ] Folder structure created (Models/, Abstractions/, SignalR/)
- [ ] ApplicationState class created with Timestamp property
- [ ] IStateSubscriber interface created (transport abstraction)
- [ ] SignalRStateSubscriber class created (implements IStateSubscriber)
- [ ] StateReceived event exposed with ApplicationState type
- [ ] Connection management implemented (ConnectAsync, DisconnectAsync)
- [ ] Automatic reconnection configured
- [ ] Project builds without errors
- [ ] Can be referenced from .NET Framework 4.8 projects

## Test

Run `dotnet build SourceCode/AgOpenGPS.Api.Client/AgOpenGPS.Api.Client.csproj` - should build successfully.
Add reference to test .NET Framework 4.8 project - should reference successfully.
