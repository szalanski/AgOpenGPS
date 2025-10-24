# Task 2: Add Timer (100ms, 10 Hz)

## Objective

Add System.Threading.Timer to ApplicationOrchestrator for main loop execution at 10 Hz.

## Context

ApplicationOrchestrator needs a timer that calls MainLoopTick() every 100ms. This task adds timer infrastructure without service calls.

See: [plan.md](plan.md) for timing requirements.

## What to Implement

### Timer Setup in ApplicationOrchestrator

**Private fields:**
- `Timer _mainLoopTimer`
- `int _tickCounter = 0`
- `DateTime _lastTickTime`
- `bool _isTickInProgress = false` (overrun detection)

**StartAsync():**
- Initialize _lastTickTime
- Create Timer with 100ms interval (from config)
- Start immediately (TimeSpan.Zero delay)
- Log startup message

**StopAsync():**
- Stop timer (Change to Timeout.Infinite)
- Log shutdown message

**MainLoopTick(object state):**
- Check if previous tick still running (overrun detection)
- Increment tick counter
- Measure delta time since last tick
- Log warning if delayed (> 1.5x interval)
- Log periodic stats (every 100 ticks)
- Catch and log exceptions

### Dispose()

- Dispose timer properly

## Acceptance Criteria

- ✅ Timer created in StartAsync()
- ✅ MainLoopTick called every 100ms
- ✅ Tick counter increments
- ✅ Delta time measured and logged
- ✅ Overrun detection prevents concurrent ticks
- ✅ Periodic stats logged (every 100 ticks = 10 seconds)
- ✅ Timer stops cleanly in StopAsync()
- ✅ Exceptions caught and logged

## Testing

Run backend and observe logs:

**Expected output (every 10 seconds):**
```
Main loop stats: Tick #100, Elapsed: 2.3 ms
Main loop stats: Tick #200, Elapsed: 2.1 ms
Main loop stats: Tick #300, Elapsed: 2.5 ms
```

**Verify frequency:**
- 100 ticks should take ~10 seconds
- If slower: Check for delays/warnings in logs

## Performance Notes

- Timer should be lightweight (< 1ms overhead)
- Async/await not needed yet (no service calls)
- Overrun detection prevents queue buildup

## References

- [03-backend-driven.md](../../architecture/03-backend-driven.md) - Backend ownership
