# Faza 1: Decyzje Techniczne (API-First)

**Data utworzenia:** 2025-10-08
**Status:** Zaakceptowany

## Architecture Decision Records (ADR)

### ADR-001: Backend Framework - ASP.NET Core Web API (.NET 8)

**Status:** ✅ Zaakceptowana

**Decyzja:** Backend jako ASP.NET Core Web API (.NET 8)

**Uzasadnienie:**
- ✅ Modern framework (LTS do listopada 2026)
- ✅ Cross-platform (Linux/macOS/Windows)
- ✅ Built-in DI, logging, configuration
- ✅ HTTP-ready (Controllers, Swagger/OpenAPI)
- ✅ Performance (Kestrel, async/await)
- ✅ Easy to deploy (standalone, Docker, cloud)

**Alternatywy odrzucone:**
- ❌ .NET Standard library - nie ma Controllers, trzeba wrapper
- ❌ .NET Framework 4.8 - Windows-only, legacy
- ❌ .NET 6 - shorter LTS (ends Nov 2024)

---

### ADR-002: Communication - In-Process (Faza 1) → HTTP (Faza 2)

**Status:** ✅ Zaakceptowana

**Decyzja:**
- Faza 1: InProcessApiClient (bezpośrednie wywołania services)
- Faza 2/M8: HttpApiClient (REST HTTP calls)

**Uzasadnienie:**
- ✅ In-process prostsze do debugowania
- ✅ In-process szybsze (no serialization, no network)
- ✅ Architektura HTTP-ready (Controllers, DTOs exist)
- ✅ Łatwe przełączenie (swap client implementation)

**Konsekwencje:**
- Backend jako ASP.NET Core Web API (ale używany in-process)
- Controllers istnieją (ale nie przez HTTP in Faza 1)
- DTOs zdefiniowane (gotowe na JSON serialization)

---

### ADR-003: API Client Library - .NET Standard 2.0

**Status:** ✅ Zaakceptowana

**Decyzja:** AgOpenGPS.Api.Client (.NET Standard 2.0)

**Uzasadnienie:**
- ✅ Kompatybilny z .NET Framework 4.8 (WinForms)
- ✅ Kompatybilny z .NET 8 (Backend)
- ✅ Interface-based (łatwa wymiana implementation)
- ✅ Maksymalna kompatybilność

**Struktura:**
```
AgOpenGPS.Api.Client/
├── IApiClient.cs (interface)
├── InProcess/InProcessApiClient.cs (Faza 1)
└── Http/HttpApiClient.cs (Faza 2)
```

---

### ADR-004: Dependency Injection - Microsoft.Extensions.DependencyInjection

**Status:** ✅ Zaakceptowana

**Decyzja:** Built-in DI z ASP.NET Core

**Uzasadnienie:**
- ✅ Standard .NET
- ✅ Lightweight
- ✅ Wystarczające dla naszych potrzeb
- ✅ No external dependencies

**Rejestracja:**
```csharp
// Backend: Program.cs
builder.Services.AddSingleton<IGuidanceService, GuidanceService>();
builder.Services.AddSingleton<IFieldService, FieldService>();
// ...

// Frontend: Program.cs
var backendServices = new ServiceCollection();
// ... (add backend services)
var backendProvider = backendServices.BuildServiceProvider();
var apiClient = new InProcessApiClient(backendProvider);
```

---

### ADR-005: Configuration Storage - JSON Files

**Status:** ✅ Zaakceptowana

**Decyzja:** JSON files zamiast Windows Registry

**Uzasadnienie:**
- ✅ Cross-platform (działa na Linux/macOS)
- ✅ Human-readable (łatwe edytowanie)
- ✅ Git-friendly (version control)
- ✅ No Windows dependencies

**Lokalizacje:**
- Windows: `%USERPROFILE%\Documents\AgOpenGPS\config.json`
- Linux: `~/.config/AgOpenGPS/config.json`
- macOS: `~/Library/Application Support/AgOpenGPS/config.json`

**Migracja:**
- Automatyczna Registry → JSON przy pierwszym uruchomieniu
- Backwards compatibility (czyta Registry jeśli JSON brak)

---

### ADR-006: Event Bus - In-Memory (Custom Implementation)

**Status:** ✅ Zaakceptowana

**Decyzja:** Custom lightweight event bus (in-memory pub/sub)

**Uzasadnienie:**
- ✅ Zero dependencies
- ✅ Simple (~100 LOC)
- ✅ Fast (in-memory)
- ✅ Wystarczające dla in-process communication

**Interface:**
```csharp
public interface IEventBus
{
    void Publish<TEvent>(TEvent @event);
    void Subscribe<TEvent>(Action<TEvent> handler);
    void Unsubscribe<TEvent>(Action<TEvent> handler);
}
```

**Future:** Jeśli potrzeba out-of-process (M8), może być SignalR/WebSocket

---

### ADR-007: DTOs (Data Transfer Objects) - Simple POCOs

**Status:** ✅ Zaakceptowana

**Decyzja:** DTOs jako simple POCOs z properties only

**Charakterystyka:**
- Properties only (no methods)
- JSON serializable
- Validation attributes (DataAnnotations)
- Conversion methods (ToDto/ToModel)

**Przykład:**
```csharp
public class GuidanceStateDto
{
    public double DistanceFromLine { get; set; }
    public double SteerAngle { get; set; }
    public Vector3Dto CurrentPosition { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

### ADR-008: Domain Models - Pure Business Logic

**Status:** ✅ Zaakceptowana

**Decyzja:** Domain Models bez dependencies (UI, HTTP, persistence)

**Charakterystyka:**
- Pure classes (data + logic)
- No UI dependencies (no System.Windows.Forms, OpenTK)
- No HTTP dependencies (no Controllers)
- Methods dla calculations

**Przykład:**
```csharp
public class ABLineModel
{
    public Vector3 PointA { get; set; }
    public Vector3 PointB { get; set; }

    // Pure calculation
    public double CalculateDistance(Vector3 position)
    {
        // Math only, no dependencies
    }
}
```

---

### ADR-009: Vector Types - Custom Structs (Domain-Specific)

**Status:** ✅ Zaakceptowana

**Decyzja:** Custom Vector3/Vector2 structs (nie OpenTK, nie System.Numerics)

**Uzasadnienie:**
- ✅ No OpenTK dependency (UI-agnostic)
- ✅ Domain-specific properties (Easting, Northing, Heading)
- ✅ Lightweight (structs)
- ✅ JSON serializable

**Implementacja:**
```csharp
public struct Vector3
{
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double Heading { get; set; }

    public double DistanceTo(Vector3 other) { /* ... */ }
}
```

**Konwersja dla rendering:**
```csharp
// Frontend może konwertować do OpenTK gdy potrzebne
public static OpenTK.Vector3 ToOpenTK(this Vector3 v)
{
    return new OpenTK.Vector3((float)v.Easting, (float)v.Northing, 0);
}
```

---

### ADR-010: Feature Flags - JSON Configuration

**Status:** ✅ Zaakceptowana

**Decyzja:** Feature flags w config.json dla przełączania legacy/new

**Struktura:**
```json
{
  "features": {
    "useApiGuidance": true,
    "useApiFields": false,
    "useApiVehicle": false
  }
}
```

**Użycie:**
```csharp
if (_featureFlags.UseApiGuidance)
{
    // New API
    return await _apiClient.Guidance.GetStateAsync();
}
else
{
    // Legacy fallback
    return _legacyAdapter.GetLegacyGuidanceState();
}
```

---

### ADR-011: Async/Await - Task-Based Async Pattern

**Status:** ✅ Zaakceptowana

**Decyzja:** Async/await dla API calls i I/O operations

**Zasady:**
- API Client methods → async
- Services I/O operations → async
- Controllers → async
- UI updates → marshal to UI thread

**Przykład:**
```csharp
// API Client
public async Task<GuidanceStateDto> GetStateAsync()
{
    return await _apiClient.Guidance.GetStateAsync();
}

// Service
public async Task<FieldModel> LoadFieldAsync(string name)
{
    var json = await File.ReadAllTextAsync(path);
    return JsonSerializer.Deserialize<FieldModel>(json);
}
```

---

### ADR-012: Serialization - System.Text.Json

**Status:** ✅ Zaakceptowana

**Decyzja:** System.Text.Json dla JSON serialization

**Uzasadnienie:**
- ✅ Modern (built-in .NET 8)
- ✅ Fast, low allocation
- ✅ Sufficient dla naszych potrzeb

**NuGet (jeśli potrzebne dla .NET Standard 2.0):**
- System.Text.Json (v8.0.0)

**Alternatywa:** Newtonsoft.Json (fallback jeśli System.Text.Json issues)

---

### ADR-013: Logging - Microsoft.Extensions.Logging

**Status:** ✅ Zaakceptowana

**Decyzja:** Built-in logging z ASP.NET Core

**Uzasadnienie:**
- ✅ Standard .NET
- ✅ Structured logging
- ✅ Multiple providers (Console, File, etc.)

**Konfiguracja:**
```csharp
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
    config.AddFile("logs/agopengps.log");
});
```

---

### ADR-014: Testing Framework - NUnit

**Status:** ✅ Zaakceptowana

**Decyzja:** NUnit (już używane w projekcie)

**Uzasadnienie:**
- ✅ Już istniejące testy w NUnit
- ✅ Familiar dla zespołu
- ✅ Good ecosystem

**Struktura:**
```
AgOpenGPS.Api.Tests/
├── Unit/
│   ├── Services/
│   └── Models/
└── Integration/
    └── Controllers/
```

---

## NuGet Dependencies Summary

### Backend (AgOpenGPS.Api - .NET 8)
```xml
<!-- Built-in with ASP.NET Core -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />

<!-- Additional if needed -->
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

### API Client (AgOpenGPS.Api.Client - .NET Standard 2.0)
```xml
<PackageReference Include="System.Text.Json" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
```

### Frontend (GPS - .NET 4.8 WinForms)
```xml
<!-- Existing -->
<PackageReference Include="OpenTK" Version="3.3.3" />

<!-- May need for API Client -->
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

---

## Performance Targets

| Operacja | Target | Uwagi |
|----------|--------|-------|
| API call (in-process) | <1ms | Direct method call |
| Guidance calculation | <10ms | 100 Hz loop |
| Field load | <500ms | Pierwsz load, later cache |
| Configuration load | <100ms | Startup |
| UI update | <16ms | 60 FPS |

---

## Dokumenty powiązane

- [phase1-strangler-fig-overview.md](phase1-strangler-fig-overview.md)
- [phase1-api-architecture.md](phase1-api-architecture.md)
- [phase1-migration-strategy.md](phase1-migration-strategy.md)
- [phase1-milestones.md](phase1-milestones.md)
