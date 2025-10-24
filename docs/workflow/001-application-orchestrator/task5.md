# Task 5: Add SignalR Broadcast

## Objective

Build complete state DTO and broadcast to all frontends via SignalR.

## Context

Final step in main loop: collect all service states and push to frontends.

See: [plan.md](plan.md) for broadcast flow.
See: [04-signalr.md](../../architecture/04-signalr.md) for communication details.

## What to Implement

### State DTO Building

After all service calls, build ApplicationStateDto:

```
ApplicationStateDto:
  ├─ VehicleStateDto (from VehicleService)
  ├─ GuidanceStateDto (from GuidanceService)
  ├─ SectionsStateDto (from SectionControlService)
  ├─ FieldStateDto (from FieldService)
  └─ Timestamp (DateTime.UtcNow)
```

Use .ToDto() extension methods on service state objects.

### SignalR Broadcast

Call:
```csharp
await _hubContext.Clients.All.SendAsync("StateUpdated", completeState);
```

This pushes state to all connected frontends.

### Performance Measurement

Measure:
- State DTO build time (target: < 5ms)
- SignalR broadcast time (target: < 20ms in-process)
- Total tick time (target: < 90ms)

Log warnings if targets exceeded.

## Acceptance Criteria

- ✅ ApplicationStateDto built every tick
- ✅ Contains all service states
- ✅ Timestamp added
- ✅ Broadcast via SignalR to all clients
- ✅ Performance measured
- ✅ Warnings logged if slow
- ✅ Broadcast happens at 10 Hz

## Testing

### Unit Test

Mock IHubContext to verify:
- StateUpdated event sent
- ApplicationStateDto passed
- All clients targeted
- Happens every tick

### Integration Test

With real SignalR hub:
- Connect test client
- Subscribe to StateUpdated
- Verify 10 messages/second received
- Verify DTO structure complete

### Manual Test

Run backend + WinForms frontend:
- Connect via SignalR
- Verify UI updates at 10 Hz
- Check browser DevTools for SignalR messages

## State DTO Structure

Ensure DTOs exist:

```csharp
public class ApplicationStateDto
{
    public VehicleStateDto Vehicle { get; set; }
    public GuidanceStateDto Guidance { get; set; }
    public SectionsStateDto Sections { get; set; }
    public FieldStateDto Field { get; set; }
    public DateTime Timestamp { get; set; }
}

public class VehicleStateDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; }
    public double Heading { get; set; }
    public double Roll { get; set; }
    // ...
}

// Similar for Guidance, Sections, Field
```

## Performance Budget

Target times:
- DTO build: < 5ms
- SignalR broadcast: < 20ms (in-process), < 50ms (HTTP)
- Total main loop: < 90ms

## Optimization Ideas (Optional)

1. **Change detection:** Only broadcast if state changed
2. **Partial updates:** Broadcast only changed properties
3. **Throttling:** Different frequencies for different state parts

Save for later optimization if needed.

## References

- [04-signalr.md](../../architecture/04-signalr.md) - SignalR architecture
- [03-backend-driven.md](../../architecture/03-backend-driven.md) - Backend push concept
