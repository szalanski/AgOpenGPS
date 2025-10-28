# Adapter Pattern and Feature Flags

## Purpose

While GNSS processing now lives in the backend, many other subsystems (guidance, section control, headland management) still run inside FormGPS. Adapters let us introduce backend services incrementally without breaking the operator workflow. Feature flags give us a safe way to switch between legacy and new code during the migration.

## Adapter Strategy

1. Wrap the legacy entry point (for example `CABLine.Calculate`) inside an adapter that exposes a clean interface.
2. Map UI state into a transport-friendly DTO when the backend service is enabled.
3. Call the backend service when the feature flag is on; otherwise, fall back to the legacy code.
4. Map backend results back into the UI model so the rest of the form remains untouched.
5. Optionally log both results during the soak period for comparison.
6. Remove the legacy branch once the backend service proves reliable.

### Minimal Example (Conceptual)

```csharp
public class GuidanceAdapter
{
    private readonly FormGPS _form;
    private readonly IGuidanceService _service;
    private readonly FeatureFlags _flags;

    public GuidanceSnapshot Calculate()
    {
        if (_flags.UseBackendGuidance)
        {
            var request = GuidanceMapper.ToRequest(_form);
            return _service.Calculate(request);
        }

        _form.Guidance.CalculateLegacy();
        return GuidanceMapper.FromLegacy(_form.Guidance);
    }
}
```

The adapter ensures callers always receive a `GuidanceSnapshot` no matter which implementation produced it.

## Feature Flags

Feature toggles live in configuration (JSON, environment variables, or any other provider supported by `Microsoft.Extensions.Options`). Each flag controls the cut-over for a specific subsystem.

```json
{
  "Features": {
    "UseBackendGuidance": true,
    "UseBackendSections": false,
    "UseBackendHeadland": false
  }
}
```

Guidelines:
- Default to the legacy path until the backend version is ready for trial.
- Allow runtime toggling when possible to simplify rollbacks.
- Log both the flag value and the adapter decision to aid diagnostics.

## Migration Stages

1. **Parallel** - Adapter dispatches to backend when the flag is on, otherwise uses the legacy implementation. Both code paths remain available.
2. **Soak** - Enable the flag for targeted users or environments, capture telemetry, and compare outputs when useful.
3. **Full cut** - Flip the flag to `true` for everyone once confidence is high.
4. **Removal** - Delete the legacy implementation and adapter branch; retire or reuse the flag.

## When to Remove Legacy Code

Only remove the fallback path when:
- The backend service has been enabled in production for an agreed soak period.
- Monitoring shows acceptable performance and correctness.
- A rollback plan is documented (even if it simply means redeploying with the flag set to `false`).
- Automated and manual tests cover the backend implementation.
- The product owner or lead developer signs off on removing the fallback.

## References

- Migration pattern: `docs/architecture/02-strangler-fig.md`
- Event-driven backend overview: `docs/architecture/03-backend-driven.md`
- Operational workflow details: `docs/implementation/sections/operational-workflows.md`
