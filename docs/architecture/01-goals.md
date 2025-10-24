# Phase 1: Goals

## End Goal (After All Phases)
- **Frontend**: Electron + React/Angular
- **Backend**: REST/gRPC API (.NET 8+)
- **Deployment**: Separate backend service + desktop/web frontend

## Phase 1 Goal
Extract business logic from WinForms to backend API:
- **Frontend**: WinForms (minimal changes, stays .NET Framework 4.8)
- **Backend**: AgOpenGPS.Api (.NET 8, API-ready services)
- **Communication**: In-process → HTTP-ready for Phase 2

## Current Problem

**FormGPS.cs (1287 lines) - Everything in one place:**
```
FormGPS
├─ UI (WinForms controls)
├─ Business Logic (guidance, field, sections)
├─ Rendering (OpenGL)
├─ Hardware (GNSS, AutoSteer, IMU)
└─ Configuration (Windows Registry)
```

**GPS/Classes/ (44 classes):**
- 19 classes with `private readonly FormGPS mf;`
- Tight coupling: `mf.vehicle`, `mf.ABLine`, `mf.tool`
- Logic mixed with FormGPS

**Consequences:**
- Cannot replace UI
- Cannot test business logic
- Cannot expose as API
- Tight coupling everywhere

## Target State (Phase 1 Complete)

**FormGPS.cs (<300 lines) - Thin UI:**
```
FormGPS
├─ SignalR Client (receives state updates)
├─ UI updates only
└─ User action handlers (button clicks)
```

**AgOpenGPS.Api (.NET 8) - Backend:**
```
AgOpenGPS.Api
├─ ApplicationOrchestrator (main loop 10 Hz)
├─ Services (pure business logic)
├─ SignalR Hub (pushes state to frontend)
└─ Models & DTOs
```

**Separation achieved:**
- Business logic → Backend
- UI → Frontend
- Communication → SignalR (in-process → HTTP)

## What Phase 1 Enables

1. **API-ready backend** - Can build new frontends (Electron, web, mobile)
2. **Testable logic** - Backend testable without WinForms
3. **Cross-platform** - Backend runs on Linux/macOS
4. **Maintainable** - Clear separation of concerns

## Independence from AgOpenGPS.Core

**We do NOT use `AgOpenGPS.Core`** (separate team's work).

| Aspect | AgOpenGPS.Core | AgOpenGPS.Api (OUR) |
|--------|----------------|---------------------|
| Team | Different team | This team |
| Pattern | MVP + WPF | Backend-driven + Strangler Fig |
| Backend | Presenters + ViewModels | ASP.NET Core + SignalR |
| Status | In progress | Planning |

**Projects:**
- ✅ `AgOpenGPS.Api/` (.NET 8) - NEW backend
- ✅ `AgOpenGPS.Api.Client/` (.NET Standard 2.0) - NEW client
- ❌ `AgOpenGPS.Core/` - NOT USED

No conflicts - completely independent initiatives.
