# Task 6: Verify and Finalize

## Goal

Verify everything works, set backend as default, document completion.

## Steps

1. Test both modes extensively
2. Verify no regressions (guidance, sections, AutoSteer, rendering)
3. Set UseBackendTick = true as default
4. Run for 1+ week in production
5. If stable: remove tmrWatchdog (optional, can wait)

## Verification Checklist

- [ ] Backend broadcasts tick every 250ms
- [ ] FormGPS receives ticks
- [ ] Guidance works
- [ ] Sections work
- [ ] AutoSteer works
- [ ] OpenGL rendering works
- [ ] No performance degradation
- [ ] Feature flag toggles correctly
- [ ] Fallback works if backend offline

## When to Delete Legacy Code

DO NOT delete until:
- Backend stable for 1+ weeks
- Zero issues reported
- Team agrees

## Completion

Foundation complete when:
- Backend controls timing
- FormGPS responds
- Zero regressions
- Feature flag works
