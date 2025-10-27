# AgOpenGPS Backend API Migration Documentation

This documentation supports **Phase 1 cross-platform migration** using the Strangler Fig Pattern.

**Goal**: Extract business logic from WinForms (GPS/Forms/FormGPS.cs) to backend API (AgOpenGPS.Api) while keeping the application running throughout the entire migration.

## Documentation Structure

This documentation is organized into **two complementary sets**:

### Set A: Architecture (Static Knowledge Base)

Small, self-contained files (~100-200 lines) that serve as **AI context** for understanding the migration architecture.

**Location**: [architecture/](architecture/)

**Files**:
1. **[01-goals.md](architecture/01-goals.md)** - Phase 1 goals, current problems, target state
2. **[02-strangler-fig.md](architecture/02-strangler-fig.md)** - Gradual migration pattern
3. **[03-backend-driven.md](architecture/03-backend-driven.md)** - Backend-driven architecture (ApplicationOrchestrator)
4. **[04-signalr.md](architecture/04-signalr.md)** - Real-time communication (Backend → Frontend push)
5. **[05-adapter-pattern.md](architecture/05-adapter-pattern.md)** - Safe migration with Feature Flags

**When to read**:
- Starting the migration
- Need to understand core patterns
- Providing context to AI tools
- Onboarding new team members

**Total reading time**: ~30-40 minutes

---

### Set B: Workflow (Task-Based Work)

Vertical slices of work organized as **workflow chunks**. Each chunk has:
- **plan.md** - High-level concept (~150-200 lines, zero code)
- **task1.md, task2.md, ...** - Independent units of work

**Location**: [workflow/](workflow/)

**Current chunks**:
1. **[001-application-orchestrator/](workflow/001-application-orchestrator/)** - ✅ COMPLETED - Backend main loop
   - plan.md - Concept overview
   - task1.md through task6.md - Backend infrastructure and SignalR state broadcasting
   - **Status**: ApplicationOrchestrator running at 4 Hz, SignalR state updates working

2. **[002-gps-gnss-migration/](workflow/002-gps-gnss-migration/)** - ✅ COMPLETED (DDD refactored) - GPS/GNSS backend migration
   - plan.md - GPS processing migration concept
   - task1.md through task9.md - GnssService, UdpPacketReceiver, Simulator, Integration Tests
   - **Status**: 41/44 integration tests passing (3 steering failures remain)
   - **Key Implementations**:
     - Event-driven ApplicationOrchestrator (processes UDP packets immediately)
     - GnssService (PGN 0xD6 unpacking, coordinate transforms)
     - SimulatorService (93ms physics tick, DDD refactored with 3 domain services)
       - VehiclePhysicsService (speed transitions, steering smoothing, heading changes, position calculations)
       - GnssDataGenerator (altitude, satellites, fix quality, HDOP, age)
       - AgIoProtocolSerializer (binary PGN 0xD6 packet serialization)
     - CQRS command pattern (Start/Stop/SetSpeed/SetSteering/Reset via MediatR)
     - SignalR bidirectional communication (state updates + commands)
     - IBackendClient abstraction for bidirectional transport

**When to use**:
- Ready to implement specific features
- Need step-by-step implementation guidance
- Working with AI-assisted development
- Breaking down complex tasks

**How it works**:
1. Read **plan.md** for high-level understanding
2. Pick a **task.md** file to work on
3. Each task is self-contained and actionable
4. Tasks reference architecture docs when needed

---

## Quick Start

### For AI Context (Understanding Architecture)

Read Set A (Architecture) files in order:
```
1. architecture/01-goals.md          (What & Why)
2. architecture/02-strangler-fig.md  (Migration pattern)
3. architecture/03-backend-driven.md (Backend ownership)
4. architecture/04-signalr.md        (Communication)
5. architecture/05-adapter-pattern.md (Safe rollout)
```

### For Implementation (Doing Work)

Pick a workflow chunk from Set B (Workflow):
```
1. Read workflow/001-application-orchestrator/plan.md
2. Pick a task (task1.md, task2.md, etc.)
3. Execute the task
4. Move to next task
```

---

## Key Architecture Patterns

### Strangler Fig Pattern
Gradually replace legacy code with new API while keeping application running. Never "big bang" rewrite.

### Backend-Driven Architecture
**Backend** owns main loop timing (ApplicationOrchestrator at 10 Hz), **Frontend** is passive receiver via SignalR.

### Adapter Pattern + Feature Flags
Wrap legacy code to delegate to new API. Feature flags allow instant rollback if issues found.

### SignalR Communication
Real-time bidirectional communication:
- **Backend → Frontend**: State updates pushed at 10 Hz
- **Frontend → Backend**: User actions via RPC calls

---

## Migration Phases

### Phase 1: Backend API (Current Work)
- Extract business logic to AgOpenGPS.Api (.NET 8)
- WinForms frontend remains (.NET Framework 4.8)
- Communication: In-process SignalR (no HTTP overhead)
- **Result**: API-ready backend, testable, cross-platform

### Phase 2: Frontend Migration (Future)
- Replace WinForms with Electron + React/Angular
- Communication: HTTP SignalR (just URL change)
- Backend unchanged
- **Result**: Modern web-based UI

---

## Independence from AgOpenGPS.Core

**IMPORTANT**: This migration is INDEPENDENT from `AgOpenGPS.Core` project.

| Aspect | AgOpenGPS.Core | AgOpenGPS.Api (OUR) |
|--------|----------------|---------------------|
| Team | Different team | This team |
| Pattern | MVP + WPF | Backend-driven + Strangler Fig |
| Backend | Presenters + ViewModels | ASP.NET Core + SignalR |
| Status | In progress | Planning |

**No conflicts** - completely separate initiatives.

---

## Contributing

When adding new workflow chunks:
1. Create `workflow/NNN-feature-name/` folder
2. Write `plan.md` (concept only, ~150-200 lines, zero code)
3. Write `taskN.md` files (independent units of work)
4. Each task should be self-contained and actionable
5. Reference architecture docs when needed

**Rules**:
- Architecture docs: Small (~100-200 lines), no code bloat
- Workflow tasks: Self-contained, links to architecture if needed
- No milestones (tasks are independent)
- No time estimates, ADRs, or success metrics
- Focus on concepts and actionable steps

---

## Additional Resources

- **[CLAUDE.md](../CLAUDE.md)** - Full project context for AI tools
- **[Simulator Architecture](simulator-architecture.md)** - Detailed technical description of the GPS/GNSS simulator
- **[Simulator Comparison](simulator-legacy-vs-api-comparison.md)** - Legacy (CSim) vs API (SimulatorService) comparison
- **[Official Docs](https://docs.agopengps.com/)** - AgOpenGPS documentation
- **[Community Forum](https://discourse.agopengps.com/)** - Discussion and support
- **GitHub Branch**: `cross-platform-support` (this migration work)
