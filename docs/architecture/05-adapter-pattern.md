# Adapter Pattern & Feature Flags

## Adapter Pattern

**Wrap legacy code to delegate to new API** while keeping legacy interface.

### Purpose
- Allow gradual migration (not big bang)
- Legacy code continues working
- New API tested alongside legacy
- Safe rollback if needed

### Concept Diagram

```
Legacy Code
  └─ Calls: guidanceAdapter.Calculate()
      │
      ▼
  Adapter (wrapper)
    └─ if (useNewApi)
          └─ Call: newGuidanceService.CalculateAsync()  ← NEW API
       else
          └─ Call: legacyGuidance.Calculate()           ← OLD CODE
```

## Example: CABLine Migration

### Stage 1: Legacy Only

```csharp
// GPS/Classes/CABLine.cs (LEGACY)
public class CABLine
{
    private readonly FormGPS mf; // Tight coupling!

    public void Calculate()
    {
        double speed = mf.avgSpeed;  // Access FormGPS
        double width = mf.tool.width;
        // ... calculations
    }
}

// GPS/Forms/FormGPS.cs
private void tmrWatchdog_Tick()
{
    ABLine.Calculate(); // Direct legacy call
}
```

### Stage 2: API + Adapter

```csharp
// Backend: AgOpenGPS.Api/Services/Guidance/ABLineService.cs (NEW)
public class ABLineService : IABLineService
{
    public ABLineState CalculateAsync(VehicleState vehicle, ToolConfig tool)
    {
        // Pure logic (no mf.xxx)
        double speed = vehicle.Speed;
        double width = tool.Width;
        // ... calculations
        return new ABLineState { ... };
    }
}

// Frontend: GPS/Adapters/ABLineAdapter.cs (TEMPORARY)
public class ABLineAdapter
{
    private readonly FormGPS _formGPS;
    private readonly IABLineService _abLineService;
    private readonly bool _useNewApi; // Feature flag!

    public ABLineState Calculate()
    {
        if (_useNewApi)
        {
            // NEW: Call API
            var vehicleState = new VehicleState {
                Speed = _formGPS.avgSpeed,
                Position = _formGPS.vehicle.pivotAxlePos.ToVector3()
            };
            var toolConfig = new ToolConfig {
                Width = _formGPS.tool.width
            };
            return _abLineService.CalculateAsync(vehicleState, toolConfig).Result;
        }
        else
        {
            // LEGACY: Fallback
            _formGPS.ABLine.Calculate();
            return ConvertLegacyToDto(_formGPS.ABLine);
        }
    }
}

// GPS/Forms/FormGPS.cs (UPDATED)
private void tmrWatchdog_Tick()
{
    var state = _abLineAdapter.Calculate(); // Through adapter
    UpdateUI(state);
}
```

### Stage 3: API Only (Adapter Removed)

```csharp
// GPS/Forms/FormGPS.cs (FINAL)
private void OnStateUpdated(ApplicationStateDto state)
{
    // Direct state from SignalR (no adapter!)
    UpdateGuidanceDisplay(state.Guidance);
}

// DELETE:
// - GPS/Classes/CABLine.cs (legacy)
// - GPS/Adapters/ABLineAdapter.cs (adapter)
```

## Feature Flags

**Toggle between legacy and new code** for safe rollout.

### Configuration

```json
// config.json
{
  "features": {
    "useApiGuidance": true,     // true = NEW, false = LEGACY
    "useApiField": false,        // false = not migrated yet
    "useApiSections": false
  }
}
```

### Implementation

```csharp
public class FeatureFlags
{
    public bool UseApiGuidance { get; set; }
    public bool UseApiField { get; set; }
    public bool UseApiSections { get; set; }
}

// Load from config
var flags = LoadFromConfig();

// Use in adapter
public ABLineAdapter(FeatureFlags flags, ...)
{
    _useNewApi = flags.UseApiGuidance;
}
```

### Benefits

- ✅ **A/B testing**: Compare legacy vs new
- ✅ **Safe rollback**: Set flag = false if broken
- ✅ **Gradual rollout**: Enable per module
- ✅ **Debug**: Run both side-by-side

## Migration Stages

### Hybrid State (Both Active)

```
FormGPS
  └─ Timer (250ms) - still active
      └─ Adapter checks flag
          ├─ flag=true  → Call new API
          └─ flag=false → Call legacy code

Backend
  └─ ApplicationOrchestrator (100ms) - already running
      └─ Broadcasts state (frontend may ignore)
```

Both systems running, feature flag chooses which is used.

### Transition (Gradual Flip)

```
Week 1: useApiGuidance = false (legacy)
Week 2: useApiGuidance = true (NEW) - testing
Week 3: useApiGuidance = true - confirmed working
Week 4: Delete legacy Guidance code
```

### Final State (API Only)

```
FormGPS
  └─ NO TIMER
      └─ SignalR only
          └─ Receives state from backend

Backend
  └─ ApplicationOrchestrator (100ms)
      └─ Complete control
```

## When to Delete Legacy

Delete legacy code ONLY when:
1. ✅ New API 100% working
2. ✅ Feature flag = true for 1+ weeks
3. ✅ Zero issues reported
4. ✅ Tests pass
5. ✅ Team agrees

**NEVER delete prematurely** - keep fallback until confident.

## References

- See: 02-strangler-fig.md (overall pattern)
- See: 03-backend-driven.md (target architecture)
