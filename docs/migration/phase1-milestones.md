# Faza 1: Milestones

**Data utworzenia:** 2025-10-08
**Status:** Zaakceptowany
**Timeline:** 8-10 tygodni

## Przegląd

| # | Milestone | Czas | Status | Zależności |
|---|-----------|------|--------|------------|
| M0 | Infrastructure Setup | 1 tydzień | ⏳ Pending | None |
| M1 | Configuration API | 1 tydzień | ⏳ Pending | M0 |
| M2 | Guidance System API | 2 tygodnie | ⏳ Pending | M0, M1 |
| M3 | Field Management API | 1 tydzień | ⏳ Pending | M0, M1 |
| M4 | Vehicle & Sections API | 1 tydzień | ⏳ Pending | M0, M2 |
| M5 | Hardware Communication API | 1 tydzień | ⏳ Pending | M0, M4 |
| M6 | Rendering Data API | 1 tydzień | ⏳ Pending | M2, M3, M4 |
| M7 | Legacy Code Cleanup | 1 tydzień | ⏳ Pending | M2-M6 |
| M8 | HTTP Ready (opcjonalnie) | 1-2 tygodnie | ⏳ Pending | M7 |

---

## M0: Infrastructure Setup

**Czas:** 1 tydzień (2-3 dni robocze)
**Zależności:** None
**Priorytet:** Krytyczny (blokuje wszystko inne)

### Cel
Stworzyć strukturę projektów Backend API + API Client, przygotować podstawową infrastrukturę (DI, logging).

### Scope
- Utworzyć projekt AgOpenGPS.Api (.NET 8 Web API)
- Utworzyć projekt AgOpenGPS.Api.Client (.NET Standard 2.0)
- Skonfigurować Dependency Injection
- Utworzyć InProcessApiClient (proof of concept)
- Utworzyć pierwszy endpoint: GET /api/health

### Deliverables
- AgOpenGPS.Api project (kompiluje się)
- AgOpenGPS.Api.Client project (kompiluje się)
- FormGPS wywołuje API Client (in-process)
- /api/health endpoint zwraca 200 OK

### Kryteria sukcesu
- ✅ Backend API uruchamia się (Kestrel)
- ✅ FormGPS uruchamia się z API Client
- ✅ FormGPS działa identycznie jak wcześniej (zero zmian funkcjonalności)
- ✅ Unit tests dla infrastructure (EventBus, DI)

### Co NIE jest w scope
- ❌ Migracja jakiejkolwiek logiki biznesowej
- ❌ HTTP implementation (tylko in-process)

**Szczegółowy plan:** Będzie utworzony gdy zaczniemy M0

---

## M1: Configuration API

**Czas:** 1 tydzień (3-4 dni robocze)
**Zależności:** M0
**Priorytet:** Wysoki (pierwszy "prawdziwy" fragment)

### Cel
Pierwszy fragment migracji - configuration service. Migracja z Windows Registry → JSON files przez API.

### Scope
- Przenieść RegistrySettings → ConfigurationService
- Utworzyć ConfigurationController (GET/PUT /api/config)
- Implementacja JsonConfigurationService (read/write JSON)
- Migracja automatyczna Registry → JSON przy pierwszym uruchomieniu
- FormGPS używa API dla configuration

### Deliverables
- ConfigurationService w Backend
- /api/config endpoints
- JSON configuration files (config.json)
- Migracja utility (Registry → JSON)
- Feature flag: useApiConfiguration

### Kryteria sukcesu
- ✅ Konfiguracja zapisywana/ładowana przez API
- ✅ Automatyczna migracja z Registry działa
- ✅ FormGPS używa API configuration
- ✅ Backwards compatibility (Registry fallback if JSON missing)
- ✅ **APLIKACJA DZIAŁA** identycznie jak wcześniej

### Co NIE jest w scope
- ❌ Migracja vehicle configuration (to later)
- ❌ Migracja field configuration (to later)

---

## M2: Guidance System API

**Czas:** 2 tygodnie (10 dni roboczych)
**Zależności:** M0, M1
**Priorytet:** Krytyczny (największy moduł)

### Cel
Migracja całego guidance system (AB Line, AB Curve, Contour, guidance calculations) do API.

### Scope
#### Week 1:
- Matematyka (vec3, vec2, glm → Vector3, Vector2, GeometryHelpers)
- AB Line service + endpoints
- Dubins path planning service

#### Week 2:
- AB Curve service + endpoints
- Contour service + endpoints
- Guidance orchestration service

### Deliverables
- GuidanceService, ABLineService, ABCurveService w Backend
- /api/guidance/* endpoints
- Feature flags per sub-module
- Adapters dla legacy code
- Unit tests + integration tests

### Kryteria sukcesu
- ✅ Guidance calculations w Backend (pure logic)
- ✅ FormGPS wywołuje API dla guidance
- ✅ Rendering w FormGPS używa DTOs z API
- ✅ Performance ±5% vs legacy
- ✅ **APLIKACJA DZIAŁA** - guidance identyczny jak wcześniej

### Co NIE jest w scope
- ❌ Tram line management (to later)
- ❌ You-turn logic (to later)

---

## M3: Field Management API

**Czas:** 1 tydzień (4-5 dni roboczych)
**Zależności:** M0, M1
**Priorytet:** Wysoki

### Cel
Migracja field management (fields, boundaries, headlands) do API.

### Scope
- FieldService (load/save/list fields)
- BoundaryService (create/edit boundaries)
- HeadlandService (headland generation)
- /api/fields/* endpoints

### Deliverables
- FieldService, BoundaryService w Backend
- /api/fields/* endpoints
- JSON persistence dla fields
- Feature flags
- Adapters

### Kryteria sukcesu
- ✅ Field management w Backend
- ✅ FormGPS używa API dla fields
- ✅ Load/save field działa
- ✅ Boundaries działają
- ✅ **APLIKACJA DZIAŁA**

---

## M4: Vehicle & Sections API

**Czas:** 1 tydzień (4-5 dni roboczych)
**Zależności:** M0, M2
**Priorytet:** Średni

### Cel
Migracja vehicle state i sections control do API.

### Scope
- VehicleService (vehicle state management)
- SectionsService (sections control logic)
- /api/vehicle/* i /api/sections/* endpoints

### Deliverables
- VehicleService, SectionsService w Backend
- API endpoints
- Feature flags
- Adapters

### Kryteria sukcesu
- ✅ Vehicle state w Backend
- ✅ Sections control przez API
- ✅ **APLIKACJA DZIAŁA**

---

## M5: Hardware Communication API

**Czas:** 1 tydzień (4-5 dni roboczych)
**Zależności:** M0, M4
**Priorytet:** Wysoki

### Cel
Migracja hardware communication (GNSS, IMU, AutoSteer) do API.

### Scope
- GnssService (NMEA parsing)
- ImuService (IMU data processing)
- AutoSteerService (autosteer communication)
- /api/hardware/* endpoints

### Deliverables
- Hardware services w Backend
- API endpoints
- Serial/UDP communication w Backend lub Frontend (TBD)

### Kryteria sukcesu
- ✅ Hardware communication w Backend
- ✅ **APLIKACJA DZIAŁA** z prawdziwym GPS

---

## M6: Rendering Data API

**Czas:** 1 tydzień (3-4 dni robocze)
**Zależności:** M2, M3, M4
**Priorytet:** Średni

### Cel
Backend dostarcza wszystkie dane do OpenGL rendering (scena 3D).

### Scope
- RenderingController (GET /api/rendering/scene)
- DTOs dla wszystkich obiektów 3D
- FormGPS rendering używa tylko DTOs

### Deliverables
- /api/rendering/scene endpoint
- Render DTOs (ABLineRenderDto, BoundaryRenderDto, etc.)

### Kryteria sukcesu
- ✅ Backend dostarcza dane sceny
- ✅ Rendering używa DTOs
- ✅ **APLIKACJA DZIAŁA** - rendering identyczny

---

## M7: Legacy Code Cleanup

**Czas:** 1 tydzień (5 dni roboczych)
**Zależności:** M2-M6
**Priorytet:** Wysoki

### Cel
Usunąć cały legacy code (GPS/Classes/*), adaptery, feature flags.

### Scope
- Usunąć 44 klasy z GPS/Classes/
- Usunąć wszystkie adaptery
- Usunąć feature flags (all = true by default)
- FormGPS.cs redukcja do <300 linii

### Deliverables
- Zero legacy code w GPS/
- FormGPS <300 LOC
- Clean architecture

### Kryteria sukcesu
- ✅ Zero GPS/Classes/
- ✅ FormGPS thin client
- ✅ **APLIKACJA DZIAŁA**

---

## M8: HTTP Ready (opcjonalnie)

**Czas:** 1-2 tygodnie (opcjonalnie)
**Zależności:** M7
**Priorytet:** Niski (nice to have)

### Cel
Przełączenie na HTTP communication (Backend jako standalone Web API).

### Scope
- Implementacja HttpApiClient
- Uruchomienie Backend jako Web API (Kestrel, port 5000)
- FormGPS konfiguracja: in-process vs HTTP
- Authentication/Authorization (if needed)

### Deliverables
- HttpApiClient implementation
- Backend jako standalone server
- FormGPS działa z HTTP backend

### Kryteria sukcesu
- ✅ Backend standalone Web API
- ✅ FormGPS działa z HTTP (lub in-process, configurable)
- ✅ **GOTOWE DO FAZY 2** (Electron + React)

---

## Zależności między milestones

```
M0 (Infrastructure)
 ├─→ M1 (Configuration)
 ├─→ M2 (Guidance) ─────┐
 │    ├─→ M4 (Vehicle) ─┼─→ M6 (Rendering)
 │    └─→ M5 (Hardware)─┘         │
 └─→ M3 (Field) ───────────────────┘
                          │
                          ▼
                     M7 (Cleanup)
                          │
                          ▼
                     M8 (HTTP - optional)
```

### Możliwe równoległe prace
- M2 i M3 można robić równolegle (różne moduły)
- M4 i M5 można robić równolegle
- M6 zależy od M2, M3, M4 (wszystkie muszą być done)

---

## Tracking progress

### Per milestone
- [ ] Szczegółowy plan utworzony
- [ ] Implementation started
- [ ] Unit tests passing
- [ ] Integration tests passing
- [ ] Manual testing done
- [ ] Code review done
- [ ] Merged to main
- [ ] ✅ Milestone COMPLETE

### Overall progress (Faza 1)
- [ ] M0 ✅ Complete
- [ ] M1 ✅ Complete
- [ ] M2 ✅ Complete
- [ ] M3 ✅ Complete
- [ ] M4 ✅ Complete
- [ ] M5 ✅ Complete
- [ ] M6 ✅ Complete
- [ ] M7 ✅ Complete
- [ ] M8 ✅ Complete (optional)

**Faza 1 ukończona gdy:** M0-M7 ✅

---

## Następne kroki

1. ✅ Przeczytać wszystkie high-level dokumenty
2. ⏳ Wybrać milestone do rozpoczęcia (rekomendacja: M0)
3. ⏳ Utworzyć szczegółowy plan dla wybranego milestone
4. ⏳ Implementować zgodnie z planem
5. ⏳ Testować, review, merge
6. ⏳ Powtórzyć dla kolejnego milestone

---

## Dokumenty powiązane

- [phase1-strangler-fig-overview.md](phase1-strangler-fig-overview.md)
- [phase1-api-architecture.md](phase1-api-architecture.md)
- [phase1-migration-strategy.md](phase1-migration-strategy.md)
- [phase1-technical-decisions.md](phase1-technical-decisions.md)

### Szczegółowe plany (tworzone per milestone)
- `milestones/m0-infrastructure-detailed.md` (gdy zaczynamy M0)
- `milestones/m1-configuration-detailed.md` (gdy zaczynamy M1)
- ... (itd.)
