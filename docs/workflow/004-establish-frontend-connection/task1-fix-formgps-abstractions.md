# Task 1: Fix FormGPS Client Abstraction References

## Goal

Update FormGPS.cs to use the correct client library abstractions (IBackendClient and BackendClientFactory) instead of the outdated references (IStateSubscriber and SubscriberFactory) that no longer exist.

## Steps

1. Locate the IStateSubscriber field declaration in FormGPS.cs (around line 91)
2. Replace IStateSubscriber type with IBackendClient type
3. Update field name if needed for clarity (optional: _stateSubscriber → _backendClient)
4. Locate the factory instantiation in FormGPS_Load method
5. Replace SubscriberFactory.CreateSignalRSubscriber call with BackendClientFactory.CreateSignalRClient
6. Update ConnectionOptions instantiation if method signature changed
7. Verify SubscribeToState method call matches IBackendClient interface signature
8. Verify OnStateReceived event handler signature matches (should already be correct)
9. Verify OnStateError event handler signature matches (should already be correct)
10. Verify Dispose call in FormGPS_FormClosing still works (should already be correct)
11. Remove any using statements for old namespaces if present
12. Add using statement for AgOpenGPS.Api.Client.Factories if needed

## Key Points

- **Minimal change**: Only 3-5 lines need updating (field type, factory call, possibly method name)
- **Event handlers unchanged**: OnStateReceived and OnStateError signatures already match IBackendClient
- **Dispose unchanged**: IBackendClient implements IDisposable just like old interface
- **Factory pattern**: BackendClientFactory creates pre-configured SignalRBackendClient instances
- **Transport abstraction**: IBackendClient hides SignalR implementation details
- **No UI changes**: Connection logic only, no visual modifications needed
- **Keep existing connection URL**: http://localhost:5000 (or from ConnectionOptions)
- **Legacy timer disabling**: Already implemented, keep as-is

## File Location

- **File to modify**: SourceCode/GPS/Forms/FormGPS.cs
- **Line references**:
  - Field declaration: ~line 91
  - Factory instantiation: In FormGPS_Load method
  - Event handlers: OnStateReceived, OnStateError methods
  - Dispose call: In FormGPS_FormClosing method

## Architecture Notes

**Old abstractions** (removed):
- IStateSubscriber interface
- SubscriberFactory class

**New abstractions** (current):
- IBackendClient interface (bidirectional: receive state + send commands)
- BackendClientFactory class (creates SignalRBackendClient)
- SignalRBackendClient implementation (hidden behind factory)

**Namespace changes**:
- Client models: AgOpenGPS.Api.Client.Models
- Client interfaces: AgOpenGPS.Api.Client.Abstractions
- Client factories: AgOpenGPS.Api.Client.Factories

## Acceptance

- [ ] IStateSubscriber replaced with IBackendClient in field declaration
- [ ] SubscriberFactory.CreateSignalRSubscriber replaced with BackendClientFactory.CreateSignalRClient
- [ ] No references to IStateSubscriber or SubscriberFactory remain in FormGPS.cs
- [ ] Using statements updated for correct namespaces
- [ ] Project builds without type errors
- [ ] No compiler warnings related to obsolete types
- [ ] Event handler signatures match IBackendClient interface
- [ ] Dispose logic still present in FormGPS_FormClosing

## Test

Build verification:
1. Build GPS project: `dotnet build SourceCode/GPS/AgOpenGPS.csproj`
2. Verify no compilation errors
3. Verify no warnings about missing types
4. Verify AgOpenGPS.Api.Client reference resolved correctly

Runtime verification (after manual testing steps in plan.md):
1. FormGPS starts without exceptions
2. Connection to backend succeeds
3. State updates received via OnStateReceived handler
4. No runtime errors related to abstractions
