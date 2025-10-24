# Backend Tick Foundation

## Goal

Replace WinForms timer with backend timer. Zero business logic changes.

## Current State

```
FormGPS → tmrWatchdog (250ms) → tmrWatchdog_tick() executes
```

## Target State

```
Backend → TickOrchestrator (250ms) → SignalR "Tick" → FormGPS → tmrWatchdog_tick() executes
```

## Why

- Proves backend can control timing
- Foundation for future state broadcasting
- Easy rollback via feature flag
- Zero risk (no logic changes)

## What This Is NOT

- NOT service orchestration (later)
- NOT state broadcasting (later)
- NOT business logic migration (later)

## Migration Path

1. **Phase 1**: Legacy only (default)
2. **Phase 2**: Hybrid (both running, feature flag chooses)
3. **Phase 3**: Backend only (delete WinForms timer)

## Tasks

1. [task1-create-api-project.md](task1-create-api-project.md) - Create .NET 8 backend
2. [task2-create-tick-orchestrator.md](task2-create-tick-orchestrator.md) - Timer that broadcasts tick
3. [task3-create-tick-hub.md](task3-create-tick-hub.md) - SignalR hub endpoint
4. [task4-connect-formgps.md](task4-connect-formgps.md) - FormGPS receives ticks
5. [task5-add-feature-flag.md](task5-add-feature-flag.md) - Toggle between old/new
6. [task6-verify-and-finalize.md](task6-verify-and-finalize.md) - Testing and completion

## Success Criteria

- Backend broadcasts tick every 250ms
- FormGPS receives tick via SignalR
- tmrWatchdog_tick() called (same logic)
- Feature flag allows toggle
- Zero regressions
