# Task 2: Display GPS Data from Backend State

## Goal

Wire FormGPS UI labels to display real-time GPS data received from backend state updates, enabling visual verification that data flows correctly from backend to frontend.

## Steps

1. Locate the OnStateReceived event handler in FormGPS.cs
2. Extract GnssState from ApplicationState parameter
3. Add null safety checks for ApplicationState.Gnss property
4. Update lblSpeed to display formatted speed from GnssState.Speed
5. Update lblFix to display fix quality information from GnssState.Quality
6. Update lblHz to display GPS message frequency from GnssState.Health.GpsHz
7. Ensure all UI updates are marshaled to UI thread using InvokeRequired check
8. Format display values appropriately for user readability
9. Add error handling for null or invalid state values
10. Test with backend simulator to verify data displays correctly

## Key Points

- **Thread safety critical**: State arrives on background thread, must use Invoke/BeginInvoke for UI updates
- **Existing UI controls**: lblSpeed, lblFix, lblHz already exist in FormGPS (no new controls needed)
- **Format guidelines**:
  - Speed: Display as "XX.X km/h" (one decimal place)
  - Fix: Display as "Fix: [quality] ([satellites] sats, age: [age]s)"
  - Hz: Display as "GPS: XX.X Hz" (one decimal place)
- **Null safety**: Check ApplicationState.Gnss is not null before accessing properties
- **Value object access**: Speed, Quality, Health are value objects with specific properties
- **No business logic**: Simple display mapping only, no calculations or transformations

## UI Control Locations

- **lblSpeed**: Located in panelControlBox (top right), Arial 21.75pt font
- **lblFix**: Located at form root level (top area), Arial 12pt font
- **lblHz**: Located in panelNavigation (left side), Tahoma 9.75pt font

## State Model Reference

**ApplicationState properties**:
- Timestamp (DateTime)
- Gnss (GnssState - may be null)

**GnssState properties** (relevant for display):
- Speed (Speed value object)
  - Speed.KilometersPerHour (double)
- Quality (GpsQuality value object)
  - Quality.FixQuality (enum: NoFix, GpsFix, DgpsFix, etc.)
  - Quality.SatellitesTracked (int)
  - Quality.Age (double - seconds)
- Health (GpsHealth value object)
  - Health.GpsHz (double - message frequency)

## Thread Safety Pattern

FormGPS UI updates must be marshaled from background thread:
1. Check InvokeRequired property
2. If true, use BeginInvoke to marshal to UI thread
3. If false, update UI directly
4. Consider creating helper method for thread-safe updates

## Error Handling

- Handle null ApplicationState gracefully
- Handle null GnssState gracefully (display "No GPS data")
- Handle invalid values (NaN, negative values) with fallback display
- Log errors for debugging without crashing UI

## Acceptance

- [ ] OnStateReceived handler extracts GnssState from ApplicationState
- [ ] lblSpeed displays speed from GnssState.Speed with proper formatting
- [ ] lblFix displays fix quality, satellite count, and age with proper formatting
- [ ] lblHz displays GPS frequency with proper formatting
- [ ] All UI updates are thread-safe (no cross-thread exceptions)
- [ ] Null safety checks prevent crashes when Gnss is null
- [ ] Display updates smoothly without flickering
- [ ] Values displayed match backend simulator output (verified manually)
- [ ] No compiler warnings or errors

