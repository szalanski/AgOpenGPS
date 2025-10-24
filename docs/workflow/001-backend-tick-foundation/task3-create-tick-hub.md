# Task 3: Create TickHub

## Goal

Create empty SignalR hub at `/hubs/tick` endpoint.

## Steps

1. Create folder: `SourceCode/AgOpenGPS.Api/Hubs/`
2. Create `TickHub.cs`: inherits from Hub
3. Add connection/disconnection logging (optional)
4. Register in Program.cs: `app.MapHub<TickHub>("/hubs/tick")`

## Key Points

- Hub is empty (no methods)
- TickOrchestrator uses IHubContext to broadcast
- Just provides endpoint

## Acceptance

- [ ] TickHub builds
- [ ] Backend runs without errors
- [ ] Endpoint /hubs/tick exists (curl returns 405, not 404)

## Test

```bash
curl -i http://localhost:5000/hubs/tick
```
Expected: 405 Method Not Allowed (normal for SignalR)
