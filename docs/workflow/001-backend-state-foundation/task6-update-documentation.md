# Task 6: Update Documentation

## Goal

Update project documentation to reflect the new backend-driven architecture with transport abstractions (IStatePublisher/IStateSubscriber), strongly-typed ApplicationState model, and SignalR implementation.

## Steps

1. Update CLAUDE.md project structure section
2. Add new projects to project structure (AgOpenGPS.Api, AgOpenGPS.Api.Client)
3. Document project folder structure (Abstractions/, Models/, Services/, Hubs/, SignalR/)
4. Document ApplicationState model (strongly-typed state)
5. Document transport abstractions (IStatePublisher, IStateSubscriber)
6. Document SignalR implementations (SignalRStatePublisher, SignalRStateSubscriber)
7. Document ApplicationOrchestrator (uses IStatePublisher)
8. Update architecture section with backend-driven pattern
9. Update building and running instructions (two-process startup)
10. Add note about workflow 001 completion
11. Update architecture/03-backend-driven.md with actual implementation notes
12. Add diagram showing abstraction layers
13. Document how to swap transports (SignalR → WebSocket/gRPC)
14. Update README.md if it exists

## Key Points

- Explain backend-driven architecture is now implemented
- **Emphasize transport abstraction layer** (easy to swap SignalR for WebSocket/gRPC)
- **Document strongly-typed ApplicationState** (not dynamic/object)
- Document how to run both processes (backend + frontend)
- List new projects and their folder structure
- Reference workflow 001 documentation
- Keep explanations concise and clear
- Focus on "what changed" and "how to use"
- Explain that SignalR is just one implementation (others can be added)

## Documentation Updates

### CLAUDE.md

Add to project structure:

**New Backend Projects:**
- `AgOpenGPS.Api/` (.NET 8) - Backend Web API with business logic
  - `Abstractions/IStatePublisher.cs` - Transport abstraction (backend)
  - `Services/ApplicationOrchestrator.cs` - Main loop (10 Hz)
  - `Services/SignalRStatePublisher.cs` - SignalR implementation
  - `Hubs/StateHub.cs` - SignalR Hub (implementation detail)

- `AgOpenGPS.Api.Client/` (.NET Standard 2.0) - Client library for FormGPS
  - `Models/ApplicationState.cs` - Strongly-typed state model
  - `Abstractions/IStateSubscriber.cs` - Transport abstraction (client)
  - `SignalR/SignalRStateSubscriber.cs` - SignalR implementation

**Architecture Changes:**
- Backend owns main loop timing (ApplicationOrchestrator at 10 Hz)
- Transport abstraction layer (IStatePublisher/IStateSubscriber)
- SignalR is one implementation (can swap for WebSocket/gRPC)
- Strongly-typed ApplicationState (not dynamic/object)
- Legacy timers (tmrWatchdog, timerSim) disabled in FormGPS

**Status Update:**
- Backend API Migration status: "Planning" → "In Progress - Workflow 001 Completed"

### architecture/03-backend-driven.md

Add "Implementation Status" section:

**Implemented Components:**
- ApplicationOrchestrator (10 Hz main loop)
- IStatePublisher abstraction
- SignalRStatePublisher implementation
- IStateSubscriber abstraction
- SignalRStateSubscriber implementation
- ApplicationState model (Timestamp property)
- StateHub (SignalR Hub)

**File Paths:**
- Backend: `SourceCode/AgOpenGPS.Api/`
- Client: `SourceCode/AgOpenGPS.Api.Client/`
- See workflow 001 documentation for detailed structure

**State Update Frequency:** 10 Hz (100ms intervals)

**Transport Swapping:**
To replace SignalR with another transport:
1. Create new implementation (e.g., `GrpcStatePublisher : IStatePublisher`)
2. Change DI: `services.AddSingleton<IStatePublisher, GrpcStatePublisher>()`
3. No changes needed in ApplicationOrchestrator or FormGPS

### architecture/06-state-model.md (New File - Optional)

Document ApplicationState structure and evolution:
- Current properties (Timestamp)
- Future properties (Gnss, Guidance, Sections)
- Serialization (System.Text.Json)
- Versioning strategy

## Running Instructions

Document the new startup procedure:

**Two-Process Architecture:**

1. **Start Backend:**
   ```bash
   dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj
   ```
   Backend runs on http://localhost:5000
   ApplicationOrchestrator starts automatically (10 Hz loop)

2. **Start Frontend:**
   ```bash
   # From Visual Studio: Run GPS project
   # Or from command line:
   dotnet run --project SourceCode/GPS/AgOpenGPS.csproj
   ```
   FormGPS connects to backend automatically via IStateSubscriber

3. **Verify Connection:**
   - Backend logs show: "ApplicationOrchestrator ticks every 100ms"
   - Backend logs show: "Client connected" when FormGPS starts
   - FormGPS UI shows: "Backend: Connected" and updating Timestamp

**Development Tips:**
- Run backend first, then frontend (easier debugging)
- Both processes run independently
- Stopping backend disconnects frontend (auto-reconnects when backend restarts)
- Legacy timers (tmrWatchdog, timerSim) are disabled

## Acceptance

- [ ] CLAUDE.md updated with new projects (AgOpenGPS.Api, AgOpenGPS.Api.Client)
- [ ] CLAUDE.md updated with folder structure (Abstractions/, Models/, Services/, Hubs/, SignalR/)
- [ ] ApplicationState model documented (strongly-typed, Timestamp property)
- [ ] Transport abstractions documented (IStatePublisher, IStateSubscriber)
- [ ] SignalR implementations documented (SignalRStatePublisher, SignalRStateSubscriber)
- [ ] ApplicationOrchestrator documented (uses IStatePublisher)
- [ ] Backend-driven architecture status updated to "In Progress"
- [ ] Running instructions added for two-process startup
- [ ] architecture/03-backend-driven.md updated with implementation status
- [ ] File paths and class names documented
- [ ] State update frequency documented (10 Hz)
- [ ] Transport swapping instructions documented
- [ ] Legacy timer replacement noted (tmrWatchdog, timerSim disabled)
- [ ] Reference to workflow 001 added
- [ ] Documentation builds and renders correctly
- [ ] All links work correctly

## Test

Read through updated documentation - should clearly explain new architecture and how to use it.
Follow running instructions - should be able to start both processes successfully.
