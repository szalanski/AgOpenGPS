# Faza 1: Architektura API

**Data utworzenia:** 2025-10-08
**Status:** Zaakceptowany

## Przegląd

Backend AgOpenGPS będzie zorganizowany jako ASP.NET Core Web API z czystą separacją warstw. W Fazie 1 API będzie wywoływane in-process (bez HTTP), ale architektura jest gotowa na łatwe przełączenie na HTTP w przyszłości.

---

## Diagram wysokopoziomowy

**ARCHITEKTURA: Backend-Driven (Backend steruje całą aplikacją)**

```
┌─────────────────────────────────────────────────────────┐
│              AgOpenGPS.Api (Backend)                    │
│                    (.NET 8)                             │
│                                                          │
│  ┌───────────────────────────────────────────────────┐  │
│  │       ApplicationOrchestrator                     │  │
│  │       (Main Loop - 10 Hz = 100ms)                 │  │
│  │                                                    │  │
│  │  1. Read Hardware (GNSS, IMU, AutoSteer)         │  │
│  │  2. Update Vehicle State                         │  │
│  │  3. Calculate Guidance                           │  │
│  │  4. Update Section Control                       │  │
│  │  5. Broadcast State via SignalR                  │  │
│  └────────────────────┬──────────────────────────────┘  │
│                       │                                  │
│                       │ Coordinates                      │
│                       ▼                                  │
│  ┌───────────────────────────────────────────────────┐  │
│  │            Business Services                      │  │
│  │  - IGnssService / GnssService                    │  │
│  │  - IVehicleService / VehicleService              │  │
│  │  - IGuidanceService / GuidanceService            │  │
│  │  - ISectionControlService / SectionControlSvc    │  │
│  │  - IFieldService / FieldService                  │  │
│  │  - IHardwareService / HardwareService            │  │
│  │  (Pure business logic, no UI)                    │  │
│  └────────────────────┬──────────────────────────────┘  │
│                       │                                  │
│                       │ Uses                             │
│                       ▼                                  │
│  ┌───────────────────────────────────────────────────┐  │
│  │            Domain Models                          │  │
│  │  - ABLineModel, ABCurveModel                     │  │
│  │  - BoundaryModel, FieldModel                     │  │
│  │  - VehicleModel, SectionModel                    │  │
│  │  - Vector3, Vector2                              │  │
│  │  (Pure domain logic, no dependencies)            │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
│  ┌───────────────────────────────────────────────────┐  │
│  │            Infrastructure                         │  │
│  │  - SignalR Hub (StateHub)                        │  │
│  │  - Persistence (JSON files, SQLite)              │  │
│  │  - Logging                                       │  │
│  └────────────────────┬──────────────────────────────┘  │
└───────────────────────┼──────────────────────────────────┘
                        │
                        │ SignalR Push (State Updates)
                        │ (Faza 1: in-process)
                        │ (Faza 2: HTTP/WebSocket)
                        ▼
┌─────────────────────────────────────────────────────────┐
│                  Frontend (WinForms)                     │
│                       (.NET 4.8)                         │
│                                                          │
│  ┌───────────────────────────────────────────────────┐  │
│  │          SignalR Client (HubConnection)           │  │
│  │  .On("StateUpdated", OnStateUpdated)             │  │
│  └────────────────────┬──────────────────────────────┘  │
│                       │                                  │
│                       │ Receives updates                 │
│                       ▼                                  │
│  ┌───────────────────────────────────────────────────┐  │
│  │            FormGPS (thin client)                  │  │
│  │  - UI Controls (buttons, labels, textboxes)      │  │
│  │  - Event handlers (user actions)                 │  │
│  │  - OpenGL Rendering (uses DTOs from backend)     │  │
│  │  - SynchronizationContext (thread-safe UI)       │  │
│  └────────────────────┬──────────────────────────────┘  │
│                       │                                  │
│                       │ User Actions                     │
│                       │ (button clicks, etc.)            │
└───────────────────────┼──────────────────────────────────┘
                        │
                        │ REST API Calls (user actions)
                        │ lub SignalR Hub.InvokeAsync()
                        ▼
┌─────────────────────────────────────────────────────────┐
│  ┌───────────────────────────────────────────────────┐  │
│  │            API Controllers (Optional)             │  │
│  │  - GuidanceController                            │  │
│  │  - FieldController                               │  │
│  │  - VehicleController                             │  │
│  │  - SectionsController                            │  │
│  │  (HTTP endpoints for user actions)               │  │
│  └────────────────────┬──────────────────────────────┘  │
│                       │                                  │
│                       │ Calls                            │
│                       ▼                                  │
│              Business Services (same as above)           │
└─────────────────────────────────────────────────────────┘
```

**Kluczowe różnice od Frontend-driven:**
- ✅ Backend ma główną pętlę (ApplicationOrchestrator)
- ✅ Backend **PUSH**uje state do frontendu (SignalR)
- ✅ Frontend **NIE MA** timera do odpytywania backendu
- ✅ Frontend tylko **wyświetla** otrzymane dane i **inicjuje** akcje użytkownika

---

## Struktura projektów

### AgOpenGPS.Api (Backend)

```
AgOpenGPS.Api/                          (.NET 8)
│
├── Program.cs                          # ASP.NET Core startup, DI config
├── appsettings.json                    # Configuration
│
├── Orchestration/                      # Main loop coordination
│   ├── ApplicationOrchestrator.cs      # IHostedService - main loop (10 Hz)
│   └── OrchestrationConfig.cs          # Loop timing, update frequencies
│
├── Hubs/                               # SignalR hubs
│   └── StateHub.cs                     # Push state updates to frontend
│
├── Controllers/                        # REST API endpoints (user actions)
│   ├── GuidanceController.cs
│   │   └── [HttpPost] /api/guidance/abline/create
│   │   └── [HttpPost] /api/guidance/abline/delete
│   ├── FieldController.cs
│   │   └── [HttpGet]  /api/fields
│   │   └── [HttpPost] /api/fields/load
│   │   └── [HttpPost] /api/fields/save
│   ├── SectionsController.cs
│   │   └── [HttpPost] /api/sections/toggle
│   ├── ConfigurationController.cs
│   │   └── [HttpGet]  /api/config
│   │   └── [HttpPut]  /api/config
│   └── HardwareController.cs           # May not be needed (backend-driven)
│
├── Services/                           # Business logic
│   ├── Guidance/
│   │   ├── IGuidanceService.cs
│   │   ├── GuidanceService.cs
│   │   ├── IABLineService.cs
│   │   └── ABLineService.cs
│   ├── Field/
│   │   ├── IFieldService.cs
│   │   ├── FieldService.cs
│   │   ├── IBoundaryService.cs
│   │   └── BoundaryService.cs
│   ├── Vehicle/
│   │   ├── IVehicleService.cs
│   │   └── VehicleService.cs
│   ├── Sections/
│   │   ├── ISectionControlService.cs
│   │   └── SectionControlService.cs
│   ├── Hardware/
│   │   ├── IGnssService.cs
│   │   ├── GnssService.cs
│   │   ├── IAutoSteerService.cs
│   │   └── AutoSteerService.cs
│   └── Configuration/
│       ├── IConfigurationService.cs
│       └── JsonConfigurationService.cs
│
├── Models/
│   ├── Domain/                         # Business models
│   │   ├── Math/
│   │   │   ├── Vector3.cs
│   │   │   ├── Vector2.cs
│   │   │   └── GeometryHelpers.cs
│   │   ├── Guidance/
│   │   │   ├── ABLineModel.cs
│   │   │   ├── ABCurveModel.cs
│   │   │   └── GuidanceState.cs
│   │   ├── Field/
│   │   │   ├── BoundaryModel.cs
│   │   │   ├── FieldModel.cs
│   │   │   └── HeadlandModel.cs
│   │   ├── Vehicle/
│   │   │   ├── VehicleModel.cs
│   │   │   ├── SectionModel.cs
│   │   │   └── ToolModel.cs
│   │   └── Hardware/
│   │       ├── GnssPosition.cs
│   │       └── ImuData.cs
│   └── Dto/                            # API contracts (JSON + SignalR)
│       ├── ApplicationStateDto.cs      # Complete state (for SignalR)
│       ├── Guidance/
│       │   ├── GuidanceStateDto.cs
│       │   ├── CreateABLineDto.cs
│       │   └── ABLineStateDto.cs
│       ├── Field/
│       │   ├── FieldDto.cs
│       │   ├── BoundaryDto.cs
│       │   └── LoadFieldRequestDto.cs
│       ├── Vehicle/
│       │   ├── VehicleStateDto.cs
│       │   └── UpdateVehicleDto.cs
│       └── Sections/
│           ├── SectionStateDto.cs
│           └── ToggleSectionDto.cs
│
├── Infrastructure/
│   ├── SignalR/
│   │   ├── InProcessSignalRConnection.cs  # In-process (Faza 1)
│   │   └── SignalROptions.cs
│   ├── Persistence/
│   │   ├── IRepository.cs
│   │   ├── JsonRepository.cs
│   │   └── FileSystem/
│   │       └── FileManager.cs
│   └── Logging/
│       └── LoggingService.cs
│
└── Extensions/
    └── ServiceCollectionExtensions.cs  # DI registration
```

### GPS (Frontend - WinForms)

```
GPS/                                    (.NET 4.8)
│
├── Forms/
│   ├── FormGPS.cs                      # Main form (thin)
│   ├── FormGPS.Designer.cs
│   ├── FormGPS_SignalR.cs              # SignalR client (partial)
│   └── FormGPS_Rendering.cs            # OpenGL rendering (partial)
│
├── SignalR/                            # SignalR client infrastructure
│   ├── IStateUpdateHandler.cs          # Interface dla state updates
│   ├── SignalRConnectionManager.cs     # Manages HubConnection
│   └── StateUpdateSubscriber.cs        # Subscribes to state updates
│
├── Rendering/                          # OpenGL renderers
│   ├── Base/
│   │   └── RendererBase.cs
│   ├── ABLineRenderer.cs               # Renderuje DTOs z backend
│   ├── BoundaryRenderer.cs
│   ├── VehicleRenderer.cs
│   └── SectionsRenderer.cs
│
├── ApiClient/                          # REST API dla user actions
│   ├── BackendApiClient.cs             # HTTP client wrapper
│   └── ApiClientFactory.cs             # Factory (in-process/HTTP)
│
└── Legacy/                             # Stopniowo usuwany
    └── Classes/                        # 44 klasy (do refaktoryzacji)
```

**Nota:** W Backend-driven architekturze:
- `AgOpenGPS.Api.Client` library może być uproszczony lub usunięty
- Frontend używa **SignalR Client** do odbierania state updates
- Frontend używa **REST API** (lub SignalR Hub methods) dla user actions

---

## Warstwy architektury

### 1. Controllers Layer

**Odpowiedzialność:**
- Przyjmuje HTTP requests (lub in-process calls)
- Walidacja input (DTOs)
- Wywołuje Services
- Zwraca HTTP responses (DTOs)

**Charakterystyka:**
- Thin layer (minimal logic)
- Zwraca standardowe HTTP codes (200, 400, 404, 500)
- Async/await
- Dependency Injection

**Przykład:**
```csharp
[ApiController]
[Route("api/guidance")]
public class GuidanceController : ControllerBase
{
    private readonly IGuidanceService _guidanceService;

    public GuidanceController(IGuidanceService guidanceService)
    {
        _guidanceService = guidanceService;
    }

    [HttpPost("update")]
    public async Task<ActionResult<GuidanceStateDto>> Update(
        [FromBody] UpdateGuidanceDto request)
    {
        var state = await _guidanceService.UpdateAsync(
            request.VehicleState.ToModel(),
            request.FieldState.ToModel()
        );

        return Ok(state.ToDto());
    }
}
```

### 2. Services Layer

**Odpowiedzialność:**
- Business logic (pure)
- Orchestracja między modelami
- Persistence (via repositories)
- Events publication

**Charakterystyka:**
- No UI dependencies
- No HTTP dependencies
- Interface-based (DI)
- Testable (unit tests)
- Async where appropriate

**Przykład:**
```csharp
public interface IGuidanceService
{
    Task<GuidanceState> UpdateAsync(VehicleState vehicle, FieldState field);
    Task CreateABLineAsync(Vector3 pointA, Vector3 pointB);
    GuidanceState GetCurrentState();
}

public class GuidanceService : IGuidanceService
{
    private readonly ABLineModel _abLine;
    private readonly IEventBus _eventBus;
    private readonly ILogger<GuidanceService> _logger;

    public async Task<GuidanceState> UpdateAsync(
        VehicleState vehicle,
        FieldState field)
    {
        // Pure business logic
        var distance = _abLine.CalculateDistance(vehicle.Position);
        var steerAngle = CalculateSteerAngle(distance, vehicle.Speed);

        var state = new GuidanceState
        {
            DistanceFromLine = distance,
            SteerAngle = steerAngle,
            Timestamp = DateTime.UtcNow
        };

        // Publish event
        await _eventBus.PublishAsync(new GuidanceUpdatedEvent(state));

        return state;
    }
}
```

### 3. Models Layer

**Domain Models:**
- Pure business logic
- No dependencies (UI, HTTP, persistence)
- Methods for calculations
- Immutable where possible

**DTOs (Data Transfer Objects):**
- API contracts (JSON serialization)
- Simple POCOs (properties only)
- Validation attributes
- Conversion methods (ToModel, ToDto)

**Przykład:**
```csharp
// Domain Model
public class ABLineModel
{
    public Vector3 PointA { get; set; }
    public Vector3 PointB { get; set; }
    public double Heading { get; set; }
    public bool IsValid { get; set; }

    // Pure calculation (no dependencies)
    public double CalculateDistance(Vector3 position)
    {
        // Math only
        // ...
    }
}

// DTO (API contract)
public class ABLineStateDto
{
    public Vector3Dto PointA { get; set; }
    public Vector3Dto PointB { get; set; }
    public double Heading { get; set; }
    public bool IsValid { get; set; }
    public double DistanceFromLine { get; set; }
    public DateTime Timestamp { get; set; }
}

// Conversion
public static class ABLineExtensions
{
    public static ABLineStateDto ToDto(this ABLineModel model)
    {
        return new ABLineStateDto
        {
            PointA = model.PointA.ToDto(),
            PointB = model.PointB.ToDto(),
            Heading = model.Heading,
            IsValid = model.IsValid,
            // ...
        };
    }
}
```

### 4. Infrastructure Layer

**EventBus:**
- Pub/Sub pattern
- Loosely coupled communication
- Later: może być SignalR (WebSocket)

**Persistence:**
- JSON files (configuration, fields)
- SQLite (optional, dla coverage maps)
- Repository pattern

**Logging:**
- Structured logging
- File + Console output

---

## API Client (Frontend ↔ Backend)

### Interface-based design

```csharp
// AgOpenGPS.Api.Client/IApiClient.cs
public interface IApiClient
{
    IGuidanceApi Guidance { get; }
    IFieldApi Fields { get; }
    IVehicleApi Vehicle { get; }
    ISectionsApi Sections { get; }
    IConfigurationApi Configuration { get; }
}

// Example sub-interface
public interface IGuidanceApi
{
    Task<GuidanceStateDto> UpdateAsync(UpdateGuidanceDto request);
    Task CreateABLineAsync(CreateABLineDto request);
    Task<ABLineStateDto> GetABLineStateAsync();
}
```

### In-process implementation (Faza 1)

```csharp
public class InProcessApiClient : IApiClient
{
    private readonly IServiceProvider _serviceProvider;

    public InProcessApiClient(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Create sub-clients
        Guidance = new InProcessGuidanceApi(serviceProvider);
        Fields = new InProcessFieldApi(serviceProvider);
        // ...
    }

    public IGuidanceApi Guidance { get; }
    public IFieldApi Fields { get; }
    // ...
}

public class InProcessGuidanceApi : IGuidanceApi
{
    private readonly IGuidanceService _guidanceService;

    public InProcessGuidanceApi(IServiceProvider serviceProvider)
    {
        _guidanceService = serviceProvider.GetRequiredService<IGuidanceService>();
    }

    public async Task<GuidanceStateDto> UpdateAsync(UpdateGuidanceDto request)
    {
        // Direct service call (no HTTP)
        var state = await _guidanceService.UpdateAsync(
            request.VehicleState.ToModel(),
            request.FieldState.ToModel()
        );

        return state.ToDto();
    }
}
```

### HTTP implementation (Faza 2 - przyszłość)

```csharp
public class HttpGuidanceApi : IGuidanceApi
{
    private readonly HttpClient _httpClient;

    public async Task<GuidanceStateDto> UpdateAsync(UpdateGuidanceDto request)
    {
        // HTTP POST
        var response = await _httpClient.PostAsJsonAsync(
            "/api/guidance/update",
            request
        );

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GuidanceStateDto>();
    }
}
```

---

## Data Flow (Backend-Driven)

### Main Loop (Backend controls timing)

```
┌─────────────────────────────────────────────────────────┐
│        ApplicationOrchestrator Main Loop (10 Hz)        │
│                   Every 100ms                           │
└─────────────────────────────────────────────────────────┘
    │
    ├─► 1. GnssService.GetLatestPositionAsync()
    │      ↓
    │   [Read GNSS data from hardware buffer]
    │
    ├─► 2. VehicleService.UpdateStateAsync(gnssData)
    │      ↓
    │   [Calculate vehicle position, heading, speed]
    │
    ├─► 3. GuidanceService.UpdateAsync(vehicleState)
    │      ↓
    │   [Calculate distance from AB line, steer angle]
    │
    ├─► 4. SectionControlService.UpdateAsync(vehicleState)
    │      ↓
    │   [Calculate which sections should be on/off]
    │
    └─► 5. StateHub.BroadcastState(completeState)
           ↓
       [SignalR sends ApplicationStateDto to all clients]
```

### Backend → Frontend (State Update Push)

```
[Backend: StateHub broadcasts state via SignalR]
    ↓
    ↓ SignalR Push (in-process lub HTTP)
    ↓
[Frontend: HubConnection.On("StateUpdated") fires]
    ↓
[SynchronizationContext.Post() → UI thread]
    ↓
[FormGPS.OnStateUpdated(ApplicationStateDto)]
    ↓
    ├─► UpdateVehicleDisplay(state.Vehicle)
    │   - lblSpeed.Text = "..."
    │   - lblHeading.Text = "..."
    │
    ├─► UpdateGuidanceDisplay(state.Guidance)
    │   - lblDistanceFromLine.Text = "..."
    │   - lblSteerAngle.Text = "..."
    │
    ├─► UpdateSectionsDisplay(state.Sections)
    │   - Section buttons colors
    │
    └─► oglMain.Invalidate()
        - Trigger OpenGL redraw
```

### Frontend → Backend (User Action)

```
[User clicks "Create AB Line" button]
    ↓
[FormGPS.btnCreateABLine_Click()]
    ↓
[Create CreateABLineDto]
    ↓
[HubConnection.InvokeAsync("CreateABLine", dto)]
    ↓ (przez istniejące SignalR connection)
    ↓
[Backend: StateHub.CreateABLine(dto)]
    ↓
[GuidanceService.CreateABLineAsync()]
    ↓
[ABLineModel updated]
    ↓
[Next main loop tick will broadcast new state automatically]
```

**Decyzja:** Używamy **SignalR Hub methods** dla user actions, ponieważ:
- ✅ SignalR connection już otwarte (brak overhead)
- ✅ Bidirectional communication przez jedno połączenie
- ✅ Brak potrzeby tworzenia nowych HTTP connections
- ✅ Spójny mechanizm (SignalR dla wszystkiego)

**REST API endpoints** pozostają opcjonalne dla:
- External integrations (inne aplikacje)
- Debugging (Swagger UI)
- Testing (Postman, curl)

### Hardware Data Flow

```
[Hardware: GNSS receiver sends NMEA via Serial/UDP]
    ↓
[Backend: GnssService receives and buffers data]
    ↓
[ApplicationOrchestrator main loop tick]
    ↓
[GnssService.GetLatestPositionAsync() returns buffered data]
    ↓
[... rest of main loop as above ...]
    ↓
[StateHub broadcasts updated state]
    ↓
[Frontend receives via SignalR, updates UI]
```

**Kluczowa różnica:** Backend **nie czeka** na frontend requests. Backend ma własny timer i **aktywnie pushuje** state updates.

---

## Dependency Injection

### Backend (AgOpenGPS.Api/Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSignalR();

// Add ApplicationOrchestrator as hosted service (main loop)
builder.Services.AddHostedService<ApplicationOrchestrator>();

// Domain models (singletons for shared state)
builder.Services.AddSingleton<ABLineModel>();
builder.Services.AddSingleton<BoundaryModel>();
builder.Services.AddSingleton<VehicleModel>();
builder.Services.AddSingleton<SectionModel[]>();

// Business services
builder.Services.AddSingleton<IGnssService, GnssService>();
builder.Services.AddSingleton<IVehicleService, VehicleService>();
builder.Services.AddSingleton<IGuidanceService, GuidanceService>();
builder.Services.AddSingleton<ISectionControlService, SectionControlService>();
builder.Services.AddSingleton<IFieldService, FieldService>();
builder.Services.AddSingleton<IConfigurationService, JsonConfigurationService>();
builder.Services.AddSingleton<IHardwareService, HardwareService>();

// Infrastructure
builder.Services.AddSingleton<IRepository, JsonRepository>();

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Map SignalR hub
app.MapHub<StateHub>("/hubs/state");

app.Run();
```

### Frontend (GPS/Program.cs) - Phase 1 (In-Process)

```csharp
static void Main()
{
    // Create backend services in-process
    var backendServices = new ServiceCollection();

    // Add SignalR (in-process)
    backendServices.AddSignalR();

    // Add ApplicationOrchestrator
    backendServices.AddHostedService<ApplicationOrchestrator>();

    // Add all backend services (same as above)
    backendServices.AddSingleton<IGnssService, GnssService>();
    // ... (wszystkie services)

    var backendProvider = backendServices.BuildServiceProvider();

    // Start ApplicationOrchestrator
    var orchestrator = backendProvider.GetRequiredService<IHostedService>();
    orchestrator.StartAsync(CancellationToken.None).Wait();

    // Create SignalR connection (in-process)
    var hubConnection = new InProcessSignalRConnection(backendProvider);

    // Run WinForms with SignalR connection
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new FormGPS(hubConnection));
}
```

### Frontend (GPS/Program.cs) - Phase 2 (HTTP)

```csharp
static void Main()
{
    // Create SignalR connection (HTTP)
    var hubConnection = new HubConnectionBuilder()
        .WithUrl("http://localhost:5000/hubs/state")
        .WithAutomaticReconnect()
        .Build();

    // Run WinForms with SignalR connection
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new FormGPS(hubConnection));
}
```

**Nota:** W Phase 2 backend działa jako oddzielny proces (ASP.NET Core app), frontend tylko się do niego podłącza.

---

## Korzyści architektury

### Separacja concerns
- ✅ Controllers = HTTP interface
- ✅ Services = Business logic
- ✅ Models = Domain logic
- ✅ DTOs = API contracts
- ✅ Infrastructure = Cross-cutting

### Testability
- ✅ Services testowane bez HTTP
- ✅ Models testowane bez Services
- ✅ Controllers testowane z mock Services
- ✅ Integration tests przez API Client

### Flexibility
- ✅ Łatwa wymiana implementacji (in-process ↔ HTTP)
- ✅ Łatwe dodawanie nowych endpoints
- ✅ Łatwe dodawanie nowych frontends
- ✅ Dependency Injection dla wszystkiego

### Maintainability
- ✅ Jasne granice odpowiedzialności
- ✅ Łatwa nawigacja w kodzie
- ✅ Łatwe dodawanie funkcji
- ✅ Łatwe refaktorowanie

---

## Dokumenty powiązane

- [phase1-strangler-fig-overview.md](phase1-strangler-fig-overview.md) - Ogólny przegląd Strangler Fig Pattern
- [phase1-backend-communication.md](phase1-backend-communication.md) - **Backend-driven architecture z SignalR**
- [phase1-backend-loop.md](phase1-backend-loop.md) - Szczegóły ApplicationOrchestrator (main loop)
- [phase1-migration-strategy.md](phase1-migration-strategy.md) - Jak migrować kod fragment po fragmencie
- [phase1-milestones.md](phase1-milestones.md) - Roadmap i timeline
- [phase1-technical-decisions-api.md](phase1-technical-decisions-api.md) - Architecture Decision Records
