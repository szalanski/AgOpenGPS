# Faza 1: Strategia Migracji

**Data utworzenia:** 2025-10-08
**Status:** Zaakceptowany

## Przegląd

Ten dokument opisuje **JAK** migrować legacy kod do nowej architektury API, zachowując działającą aplikację na każdym etapie.

---

## Zasada #1: Fragment po fragmencie

### NIE robimy:
❌ Przepisać cały system na raz ("big bang")
❌ Stworzyć równoległy system i przełączyć się
❌ Zablokować development na 3 miesiące

### ROBIMY:
✅ Wybieramy **jeden fragment** (np. Guidance Calculations)
✅ Migrujemy ten fragment do API
✅ **Aplikacja działa** z tym fragmentem w API
✅ Powtarzamy dla kolejnego fragmentu

### Przykład: Guidance Module

**Iteracja 1:**
- Guidance w API ✅
- Field legacy ❌
- Vehicle legacy ❌
- **APLIKACJA DZIAŁA**

**Iteracja 2:**
- Guidance w API ✅
- Field w API ✅
- Vehicle legacy ❌
- **APLIKACJA DZIAŁA**

**Iteracja N:**
- Wszystko w API ✅
- Zero legacy ✅
- **APLIKACJA DZIAŁA**

---

## Wzorzec: Adapter Pattern

### Problem
Legacy kod (CABLine) zależy od FormGPS (`mf.vehicle`, `mf.tool`).

### Rozwiązanie
Tworzymy **adapter**, który pozwala legacy code współpracować z nowym API.

### Etap 1: Legacy tylko

```csharp
// GPS/Classes/CABLine.cs (legacy)
public class CABLine
{
    private readonly FormGPS mf; // ❌ Tight coupling

    public void Calculate()
    {
        double width = mf.tool.width;
        double speed = mf.avgSpeed;
        // ... logic
    }
}

// GPS/Forms/FormGPS.cs
public class FormGPS : Form
{
    public CABLine ABLine;

    private void Timer_Tick()
    {
        ABLine.Calculate(); // Legacy way
    }
}
```

### Etap 2: API + Adapter + Legacy

```csharp
// Backend: AgOpenGPS.Api/Services/ABLineService.cs (NEW)
public class ABLineService : IABLineService
{
    public ABLineState Calculate(VehicleState vehicle, ToolConfiguration tool)
    {
        // Pure logic (no mf.xxx)
        double width = tool.Width;
        double speed = vehicle.Speed;
        // ...
        return new ABLineState { ... };
    }
}

// GPS/Legacy/ABLineAdapter.cs (TEMPORARY)
public class ABLineAdapter
{
    private readonly FormGPS _formGPS; // Legacy
    private readonly IABLineService _abLineService; // New
    private readonly bool _useNewApi; // Feature flag

    public ABLineState Calculate()
    {
        if (_useNewApi)
        {
            // ✅ NEW: Call API
            var vehicleState = new VehicleState
            {
                Speed = _formGPS.avgSpeed,
                Position = _formGPS.vehicle.pivotAxlePos.ToVector3()
            };
            var toolConfig = new ToolConfiguration
            {
                Width = _formGPS.tool.width,
                Overlap = _formGPS.tool.overlap
            };
            return _abLineService.Calculate(vehicleState, toolConfig);
        }
        else
        {
            // ❌ LEGACY: Fallback
            _formGPS.ABLine.Calculate();
            return new ABLineState
            {
                // Convert legacy state to DTO
                // ...
            };
        }
    }
}

// GPS/Forms/FormGPS.cs
public class FormGPS : Form
{
    private ABLineAdapter _abLineAdapter;

    private void Timer_Tick()
    {
        var state = _abLineAdapter.Calculate(); // Through adapter
        UpdateUI(state);
    }
}
```

### Etap 3: API only (adapter usunięty)

```csharp
// GPS/Forms/FormGPS.cs
public class FormGPS : Form
{
    private readonly IApiClient _apiClient;

    private async void Timer_Tick()
    {
        // ✅ Direct API call
        var vehicleState = GetCurrentVehicleState();
        var toolConfig = GetToolConfiguration();

        var state = await _apiClient.Guidance.CalculateABLineAsync(
            vehicleState,
            toolConfig
        );

        UpdateUI(state);
    }
}

// GPS/Classes/CABLine.cs - DELETED
// GPS/Legacy/ABLineAdapter.cs - DELETED
```

---

## Feature Flags

### Konfiguracja

```json
// config.json
{
  "features": {
    "guidance": {
      "useApiGuidance": true,      // true = new API, false = legacy
      "useApiABLine": true,
      "useApiABCurve": false       // false = not migrated yet
    },
    "field": {
      "useApiFields": false,        // false = legacy
      "useApiBoundaries": false
    }
  }
}
```

### Implementacja

```csharp
// GPS/ApiClient/BackendApiClient.cs
public class BackendApiClient
{
    private readonly FeatureFlags _flags;
    private readonly IApiClient _apiClient;
    private readonly LegacyCodeAdapter _legacyAdapter;

    public async Task<GuidanceState> GetGuidanceStateAsync()
    {
        if (_flags.UseApiGuidance)
        {
            // ✅ NEW API
            return await _apiClient.Guidance.GetStateAsync();
        }
        else
        {
            // ❌ LEGACY fallback
            return _legacyAdapter.GetLegacyGuidanceState();
        }
    }
}
```

### Korzyści
- ✅ A/B testing (porównanie legacy vs new)
- ✅ Rollback w razie problemów (feature flag = false)
- ✅ Stopniowe włączanie dla użytkowników
- ✅ Bezpieczne testowanie

---

## Proces refaktoryzacji: Krok po kroku

### Przykład: Refaktoryzacja CABLine

#### Krok 1: Analiza

**Cel:** Zrozumieć co robi CABLine

1. Przeczytać kod CABLine
2. Znaleźć wszystkie dependencies (mf.xxx)
3. Zidentyfikować pure logic vs UI logic
4. Zidentyfikować rendering code

**Output:** Lista dependencies i logiki do migracji

#### Krok 2: Utworzenie modelu domenowego

**Cel:** Pure business logic bez dependencies

```csharp
// Backend: AgOpenGPS.Api/Models/Domain/Guidance/ABLineModel.cs
public class ABLineModel
{
    public Vector3 PointA { get; set; }
    public Vector3 PointB { get; set; }
    public double Heading { get; set; }
    public bool IsValid { get; set; }

    // Pure calculations (no mf.xxx)
    public double CalculateDistance(Vector3 position)
    {
        // Math only - copied from CABLine
        // But without mf.xxx references
        // ...
    }
}
```

**Test:** Unit test dla ABLineModel

#### Krok 3: Utworzenie service

**Cel:** Business logic orchestration

```csharp
// Backend: AgOpenGPS.Api/Services/Guidance/ABLineService.cs
public interface IABLineService
{
    void CreateABLine(Vector3 pointA, Vector3 pointB);
    ABLineState GetCurrentState();
    ABLineState Calculate(VehicleState vehicle, ToolConfiguration tool);
}

public class ABLineService : IABLineService
{
    private readonly ABLineModel _model;
    private readonly IEventBus _eventBus;

    public ABLineState Calculate(VehicleState vehicle, ToolConfiguration tool)
    {
        // Use model
        var distance = _model.CalculateDistance(vehicle.Position);

        // Additional logic
        // ...

        var state = new ABLineState
        {
            DistanceFromLine = distance,
            // ...
        };

        // Publish event
        _eventBus.Publish(new ABLineUpdatedEvent(state));

        return state;
    }
}
```

**Test:** Unit test dla ABLineService (mocked dependencies)

#### Krok 4: Utworzenie API Controller

**Cel:** HTTP endpoint (HTTP-ready)

```csharp
// Backend: AgOpenGPS.Api/Controllers/GuidanceController.cs
[ApiController]
[Route("api/guidance/abline")]
public class ABLineController : ControllerBase
{
    private readonly IABLineService _abLineService;

    [HttpPost("calculate")]
    public ActionResult<ABLineStateDto> Calculate([FromBody] CalculateABLineDto request)
    {
        var state = _abLineService.Calculate(
            request.VehicleState.ToModel(),
            request.ToolConfiguration.ToModel()
        );

        return Ok(state.ToDto());
    }
}
```

**Test:** Integration test (call controller, verify response)

#### Krok 5: Utworzenie adaptera (temporary)

**Cel:** Łącznik między legacy a new

```csharp
// GPS/Legacy/ABLineAdapter.cs
public class ABLineAdapter
{
    private readonly FormGPS _formGPS;
    private readonly IApiClient _apiClient;
    private readonly FeatureFlags _flags;

    public ABLineState Calculate()
    {
        if (_flags.UseApiABLine)
        {
            return CallNewApi();
        }
        else
        {
            return CallLegacyCode();
        }
    }

    private ABLineState CallNewApi()
    {
        var vehicleState = new VehicleState
        {
            Position = _formGPS.vehicle.pivotAxlePos.ToVector3(),
            Speed = _formGPS.avgSpeed,
            // ...
        };

        return _apiClient.Guidance.CalculateABLineAsync(vehicleState).Result;
    }

    private ABLineState CallLegacyCode()
    {
        _formGPS.ABLine.Calculate();
        return new ABLineState
        {
            // Convert from legacy
            // ...
        };
    }
}
```

#### Krok 6: Aktualizacja FormGPS

**Cel:** Używać adaptera zamiast bezpośrednio legacy

```csharp
// GPS/Forms/FormGPS.cs
public class FormGPS : Form
{
    // private CABLine ABLine; // ❌ OLD - commented out
    private ABLineAdapter _abLineAdapter; // ✅ NEW

    private void InitializeABLine()
    {
        _abLineAdapter = new ABLineAdapter(this, _apiClient, _featureFlags);
    }

    private void Timer_Tick()
    {
        // var state = ABLine.Calculate(); // ❌ OLD
        var state = _abLineAdapter.Calculate(); // ✅ NEW
        UpdateUI(state);
    }
}
```

**Test:** Manual testing - aplikacja musi działać!

#### Krok 7: Testing z feature flag

**Test plan:**
1. Feature flag = false → test legacy code działa
2. Feature flag = true → test new API działa
3. Porównanie wyników (muszą być identyczne)

**Jeśli new API działa:**
✅ Commit: "feat: ABLine API implementation (behind feature flag)"

**Jeśli nie działa:**
❌ Debug, fix, repeat tests

#### Krok 8: Usunięcie legacy code

**Gdy new API działa stabilnie (np. 1-2 tygodnie):**

```csharp
// GPS/Forms/FormGPS.cs
public class FormGPS : Form
{
    private readonly IApiClient _apiClient;

    private async void Timer_Tick()
    {
        // ✅ Direct API call (no adapter)
        var vehicleState = GetCurrentVehicleState();
        var state = await _apiClient.Guidance.CalculateABLineAsync(vehicleState);
        UpdateUI(state);
    }
}

// Delete:
// - GPS/Classes/CABLine.cs (legacy)
// - GPS/Legacy/ABLineAdapter.cs (adapter)
```

**Test:** Manual testing - aplikacja musi działać!

✅ Commit: "refactor: Remove legacy ABLine code"

---

## Testing Strategy

### Po każdym kroku refaktoryzacji

#### 1. Unit Tests
```csharp
[TestFixture]
public class ABLineModelTests
{
    [Test]
    public void CalculateDistance_WithValidLine_ReturnsCorrectDistance()
    {
        // Arrange
        var model = new ABLineModel
        {
            PointA = new Vector3(0, 0, 0),
            PointB = new Vector3(100, 0, 0)
        };
        var position = new Vector3(50, 10, 0);

        // Act
        var distance = model.CalculateDistance(position);

        // Assert
        Assert.AreEqual(10.0, distance, 0.01);
    }
}
```

#### 2. Integration Tests
```csharp
[TestFixture]
public class ABLineApiTests
{
    private IApiClient _apiClient;

    [SetUp]
    public void SetUp()
    {
        // Setup in-process API client
        _apiClient = CreateInProcessApiClient();
    }

    [Test]
    public async Task CalculateABLine_ReturnsValidState()
    {
        // Arrange
        var vehicleState = new VehicleState { /* ... */ };

        // Act
        var result = await _apiClient.Guidance.CalculateABLineAsync(vehicleState);

        // Assert
        Assert.IsNotNull(result);
        Assert.That(result.DistanceFromLine, Is.GreaterThanOrEqualTo(0));
    }
}
```

#### 3. Golden Master Tests (Regression)
```csharp
[TestFixture]
public class GuidanceRegressionTests
{
    [Test]
    public void NewApi_MatchesLegacyResults()
    {
        // Arrange
        var testCases = LoadTestCases("guidance_test_data.json");

        foreach (var testCase in testCases)
        {
            // Act - Legacy
            var legacyResult = RunLegacyGuidance(testCase.Input);

            // Act - New API
            var newResult = RunNewApiGuidance(testCase.Input);

            // Assert
            Assert.AreEqual(legacyResult.DistanceFromLine, newResult.DistanceFromLine, 0.01);
            Assert.AreEqual(legacyResult.SteerAngle, newResult.SteerAngle, 0.1);
        }
    }
}
```

#### 4. Manual Testing
- Uruchomić FormGPS
- Utworzyć AB Line
- Sprawdzić czy guidance działa
- Sprawdzić czy rendering działa
- Sprawdzić performance (nie wolniejsze)

---

## Migracja Timer → ApplicationOrchestrator (Backend-Driven)

### Problem

Obecna aplikacja ma timer w FormGPS (`tmrWatchdog`) który kontroluje cały flow:

```csharp
// GPS/Forms/FormGPS.cs - CURRENT
private void tmrWatchdog_Tick(object sender, EventArgs e)
{
    // Frontend kontroluje timing!
    pn.UpdatePosition();
    guidanceLine.UpdateGuidanceLine();
    section.UpdateSections();

    // UI updates
    lblSpeed.Text = pn.speed.ToString();
    oglMain.Invalidate();
}
```

**Problemy:**
- ❌ Frontend kontroluje timing (250ms)
- ❌ Business logic mixed z UI updates
- ❌ Niemożliwe użycie bez WinForms

### Rozwiązanie: Backend-Driven z ApplicationOrchestrator

#### Krok 1: Utworzenie ApplicationOrchestrator

```csharp
// AgOpenGPS.Api/Orchestration/ApplicationOrchestrator.cs
public class ApplicationOrchestrator : IHostedService
{
    private Timer _mainLoopTimer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _mainLoopTimer = new Timer(
            MainLoopTick,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(100)); // 10 Hz

        return Task.CompletedTask;
    }

    private async void MainLoopTick(object state)
    {
        // 1. Read hardware
        var gnssData = await _gnssService.GetLatestPositionAsync();

        // 2. Update vehicle
        var vehicleState = await _vehicleService.UpdateStateAsync(gnssData);

        // 3. Calculate guidance
        var guidanceState = await _guidanceService.UpdateAsync(vehicleState);

        // 4. Update sections
        var sectionsState = await _sectionService.UpdateAsync(vehicleState);

        // 5. Build complete state
        var completeState = new ApplicationStateDto
        {
            Vehicle = vehicleState.ToDto(),
            Guidance = guidanceState.ToDto(),
            Sections = sectionsState.ToDto()
        };

        // 6. Broadcast via SignalR
        await _hubContext.Clients.All.SendAsync("StateUpdated", completeState);
    }
}
```

#### Krok 2: Dodanie SignalR Client do FormGPS

```csharp
// GPS/Forms/FormGPS.cs - NEW
public partial class FormGPS : Form
{
    private HubConnection _hubConnection;
    private readonly SynchronizationContext _uiContext;

    public FormGPS()
    {
        InitializeComponent();

        _uiContext = SynchronizationContext.Current;

        InitializeSignalRConnection();
    }

    private void InitializeSignalRConnection()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/hubs/state") // Phase 2: HTTP
            // .WithInProcessConnection(backendProvider)  // Phase 1: in-process
            .WithAutomaticReconnect()
            .Build();

        // Register handler for state updates
        _hubConnection.On<ApplicationStateDto>("StateUpdated", OnStateUpdated);
    }

    private async void FormGPS_Load(object sender, EventArgs e)
    {
        await _hubConnection.StartAsync();
    }

    private void OnStateUpdated(ApplicationStateDto state)
    {
        // Marshal to UI thread
        _uiContext.Post(_ =>
        {
            // TYLKO UI updates (no business logic!)
            UpdateVehicleDisplay(state.Vehicle);
            UpdateGuidanceDisplay(state.Guidance);
            UpdateSectionsDisplay(state.Sections);

            oglMain.Invalidate();
        }, null);
    }

    private void UpdateVehicleDisplay(VehicleStateDto vehicle)
    {
        lblSpeed.Text = vehicle.Speed.ToString("F1");
        lblHeading.Text = vehicle.Heading.ToString("F1");
        lblLatitude.Text = vehicle.Position.Latitude.ToString("F7");
    }

    private void UpdateGuidanceDisplay(GuidanceStateDto guidance)
    {
        lblDistanceFromLine.Text = guidance.DistanceFromLine.ToString("F2");
        lblSteerAngle.Text = guidance.SteerAngle.ToString("F1");
    }

    private void UpdateSectionsDisplay(SectionStateDto[] sections)
    {
        for (int i = 0; i < sections.Length; i++)
        {
            _sectionButtons[i].BackColor = sections[i].IsOn ? Color.Green : Color.Red;
        }
    }
}
```

#### Krok 3: Stopniowa migracja z Adapter Pattern

**Phase A: Hybrydowy - Timer + ApplicationOrchestrator**

```csharp
// GPS/Forms/FormGPS.cs - HYBRID
private bool _useBackendLoop = false; // Feature flag

private void tmrWatchdog_Tick(object sender, EventArgs e)
{
    if (_useBackendLoop)
    {
        // ✅ NEW: Backend-driven (nothing to do, SignalR handles it)
        return;
    }
    else
    {
        // ❌ LEGACY: Frontend-driven
        pn.UpdatePosition();
        guidanceLine.UpdateGuidanceLine();
        UpdateUI();
    }
}
```

**Phase B: Feature flag = true, Timer wyłączony**

```csharp
// GPS/Forms/FormGPS.cs
public FormGPS()
{
    InitializeComponent();

    if (_useBackendLoop)
    {
        // ✅ Backend-driven: disable timer
        tmrWatchdog.Enabled = false;

        // SignalR will trigger updates
        InitializeSignalRConnection();
    }
    else
    {
        // ❌ Legacy: use timer
        tmrWatchdog.Enabled = true;
        tmrWatchdog.Interval = 250;
    }
}
```

**Phase C: Timer usunięty całkowicie**

```csharp
// GPS/Forms/FormGPS.cs - FINAL
// tmrWatchdog DELETED from Designer
// tmrWatchdog_Tick() method DELETED

// Only SignalR handler remains:
private void OnStateUpdated(ApplicationStateDto state)
{
    _uiContext.Post(_ =>
    {
        UpdateVehicleDisplay(state.Vehicle);
        UpdateGuidanceDisplay(state.Guidance);
        oglMain.Invalidate();
    }, null);
}
```

### Korzyści Backend-Driven

| Aspect | Frontend-Driven (old) | Backend-Driven (new) |
|--------|----------------------|----------------------|
| **Control** | Frontend timer (250ms) | Backend loop (100ms) |
| **Logic** | Mixed in Timer_Tick | Separated in Services |
| **Testing** | Hard (needs FormGPS) | Easy (unit tests) |
| **Multi-frontend** | Impossible | Easy (WinForms, Electron, mobile) |
| **Performance** | UI thread blocking | Background thread |
| **Scalability** | 1 frontend only | N frontends (broadcast) |

### Checklist: Migracja Timer → ApplicationOrchestrator

#### Backend
- ✅ Utworzyć `ApplicationOrchestrator.cs`
- ✅ Dodać `StateHub.cs` (SignalR)
- ✅ Dodać `ApplicationStateDto.cs`
- ✅ Zarejestrować `AddHostedService<ApplicationOrchestrator>()`
- ✅ Zarejestrować `AddSignalR()` i `MapHub<StateHub>()`
- ✅ Przetestować backend standalone (bez frontendu)

#### Frontend
- ✅ Dodać NuGet: `Microsoft.AspNetCore.SignalR.Client`
- ✅ Utworzyć `FormGPS_SignalR.cs` (partial class)
- ✅ Dodać `HubConnection` setup
- ✅ Dodać `OnStateUpdated()` handler
- ✅ Przenieść UI updates z `Timer_Tick` do `OnStateUpdated`
- ✅ Dodać feature flag `useBackendLoop`
- ✅ Przetestować z feature flag = true/false

#### Migracja
- ✅ Phase A: Hybrydowy (timer + SignalR)
- ✅ Phase B: Feature flag = true (tylko SignalR)
- ✅ Phase C: Usunąć timer całkowicie
- ✅ Usunąć legacy code (tmrWatchdog, Timer_Tick)

#### Testing
- ✅ Unit tests dla ApplicationOrchestrator
- ✅ Integration test (backend loop → SignalR → frontend)
- ✅ Performance test (timing, throughput)
- ✅ Manual test (FormGPS z feature flag on/off)

---

## Rollback Strategy

### Jeśli coś się psuje

#### Opcja 1: Feature Flag rollback
```json
// config.json - przełączyć flag na false
{
  "features": {
    "useApiABLine": false  // ← Rollback to legacy
  }
}
```

#### Opcja 2: Git revert
```bash
git log --oneline -10  # Find problematic commit
git revert <commit-hash>
```

#### Opcja 3: Branch rollback
```bash
git checkout main
git reset --hard <last-good-commit>
git push --force
```

### Zasady rollback
- ⏱️ **Quick decision** - jeśli broken, rollback w ciągu 1h
- 🧪 **Test before merge** - lepiej catch przed merge
- 📝 **Document issues** - note co się popsuło, dlaczego

---

## Checklist: Przed merge'em każdego fragmentu

### Code
- ✅ Kod kompiluje się bez warnings
- ✅ Unit tests przechodzą
- ✅ Integration tests przechodzą
- ✅ Golden master tests przechodzą (if applicable)
- ✅ Code review done
- ✅ No hardcoded values

### Testing
- ✅ Manual testing done (FormGPS)
- ✅ Feature flag tested (on/off)
- ✅ Performance tested (no degradation)
- ✅ Memory usage checked (no leaks)

### Documentation
- ✅ Code commented where needed
- ✅ API endpoint documented (Swagger/OpenAPI)
- ✅ Migration notes updated

### Deployment
- ✅ Feature flag default = false (safe rollout)
- ✅ Rollback plan documented
- ✅ Monitoring ready (logs, errors)

---

## Dobre praktyki

### DO
✅ Migruj małe fragmenty (1-2 klasy na raz)
✅ Testuj po każdym kroku
✅ Używaj feature flags
✅ Commit często (working states)
✅ Code review przed merge
✅ Dokumentuj decyzje

### DON'T
❌ Nie migruj wszystkiego naraz
❌ Nie merge bez testów
❌ Nie usuwaj legacy code od razu
❌ Nie hardcode values
❌ Nie skip code review
❌ Nie zostawiaj dead code

---

## Dokumenty powiązane

- [phase1-strangler-fig-overview.md](phase1-strangler-fig-overview.md)
- [phase1-api-architecture.md](phase1-api-architecture.md)
- [phase1-milestones.md](phase1-milestones.md)
- [phase1-technical-decisions.md](phase1-technical-decisions.md)
