# Task 8: Frontend Use Backend GNSS

## Goal

Update FormGPS to receive and display GPS state from backend instead of processing UDP packets locally.

## Steps

1. Add field in FormGPS to store latest ApplicationState
2. Update OnStateReceived to cache received ApplicationState
3. Remove or comment out UDP packet processing in UDPComm.Designer.cs
4. Update Position.designer.cs to read from cached state.Gnss instead of pn fields
5. Update UI displays (speed, heading, position labels) to use state.Gnss
6. Update GPS quality indicators to use state.Gnss fix quality
7. Decide whether to keep CNMEA class as thin adapter or remove entirely
8. Add graceful handling if backend disconnects (show warning, no GPS data)
9. Test with backend running - verify UI displays GPS data
10. Test behavior matches original implementation

## Key Points

- FormGPS becomes GPS consumer instead of processor
- OnStateReceived caches latest state for UI thread access
- UDP processing removed from FormGPS (backend handles it now)
- All pn.fix, pn.speed, pn.heading references changed to state.Gnss
- CNMEA class may be kept as adapter initially for safety
- Must handle disconnection gracefully (backend required for GPS)
- UI behavior should match previous implementation exactly

## Acceptance

- [ ] FormGPS stores latest ApplicationState
- [ ] OnStateReceived caches state
- [ ] UDP processing removed from UDPComm.Designer.cs
- [ ] Position.designer.cs uses state.Gnss instead of pn fields
- [ ] UI displays show GPS data from backend
- [ ] GPS quality indicators use backend data
- [ ] Disconnection handled gracefully
- [ ] GPS project builds successfully
- [ ] UI behavior matches previous implementation

## Test

Run backend and FormGPS - UI displays GPS data from backend, behavior matches original.
