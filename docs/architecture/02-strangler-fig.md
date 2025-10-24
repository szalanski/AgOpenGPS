# Strangler Fig Pattern

## Pattern Explanation

**Strangler Fig** = Gradual migration of legacy system where:
1. New code wraps old code (doesn't replace immediately)
2. Gradually redirect functionality to new code
3. Application works throughout entire migration
4. Delete old code only when 100% replaced

Named after "strangler fig" plant that grows around a tree and gradually replaces it.

## Transformation Stages

### Stage 0: Initial State
```
┌─────────────────────────────┐
│      FormGPS (WinForms)     │
│  Everything in one place    │
└─────────────────────────────┘
```

### Stage N: Intermediate (during migration)
```
┌─────────────────────────────┐
│   FormGPS (WinForms - thin) │
│   - UI + Rendering          │
│   - SignalR Client          │
└──────────┬──────────────────┘
           │ In-process
           ▼
┌─────────────────────────────┐
│     AgOpenGPS.Api (.NET 8)  │
│   ✅ Part migrated          │
│   ✅ Services + API         │
└──────────┬──────────────────┘
           │
           ▼
      ┌─────────┐
      │ Legacy  │ ← Gradually removed
      └─────────┘
```

### Stage Final: Phase 1 Complete
```
┌─────────────────────────────┐
│   FormGPS (<300 lines)      │
│   - UI only                 │
│   - SignalR Client          │
└──────────┬──────────────────┘
           │ In-process/HTTP
           ▼
┌─────────────────────────────┐
│    AgOpenGPS.Api (.NET 8)   │
│  ✅ 100% business logic     │
│  ✅ API-ready               │
│  ✅ Zero legacy             │
└─────────────────────────────┘
```

## Key Principle

**Application MUST work after every change.**

After each commit/merge:
- ✅ Code compiles
- ✅ FormGPS runs
- ✅ All features work
- ✅ Tests pass
- ✅ Zero regression

If broken → **ROLLBACK immediately**.

## Safety Mechanisms

### 1. Feature Flags
Toggle between legacy/new code:
```csharp
if (_useNewApi)
    return await _apiClient.GetGuidance(); // NEW
else
    return _legacyGuidance.Calculate();    // OLD (fallback)
```

### 2. Adapter Pattern
Legacy code calls new API:
```csharp
// Legacy code thinks it's calling old code
var result = _adapter.Calculate();

// Adapter delegates to new API
class Adapter {
    Calculate() {
        return _newApiService.CalculateAsync().Result;
    }
}
```

### 3. Incremental Rollout
- Migrate ONE module at a time
- Test thoroughly before next module
- Each module = independent vertical slice

## Migration Fragment by Fragment

**NOT:**
- ❌ Big bang rewrite
- ❌ Parallel system (old + new running simultaneously)
- ❌ Block development for months

**YES:**
- ✅ One fragment (e.g., Guidance calculations)
- ✅ Migrate fragment to API
- ✅ Application works with that fragment in API
- ✅ Repeat for next fragment

**Example progression:**
```
Iteration 1:
  Guidance in API ✅
  Field legacy ❌
  Vehicle legacy ❌
  → APP WORKS

Iteration 2:
  Guidance in API ✅
  Field in API ✅
  Vehicle legacy ❌
  → APP WORKS

Iteration N:
  Everything in API ✅
  Zero legacy ✅
  → APP WORKS
```

## Rollback Strategy

If something breaks:

**Option 1: Feature Flag**
```json
{ "useApiGuidance": false }  // Toggle back to legacy
```

**Option 2: Git Revert**
```bash
git revert <commit-hash>
```

**Rule**: If broken, rollback within 1 hour. Don't merge broken code.

## References

- See: 01-goals.md (what we're achieving)
- See: 05-adapter-pattern.md (how to wrap legacy code)
