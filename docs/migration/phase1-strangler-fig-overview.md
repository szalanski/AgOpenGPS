# Faza 1: Przegląd Migracji - Strangler Fig Pattern

**Data utworzenia:** 2025-10-08
**Status:** Zaakceptowany
**Autor:** Team AgOpenGPS

## Executive Summary

### Cel końcowy (po wszystkich fazach)
- **Frontend:** Electron + React/Angular (modern web UI)
- **Backend:** REST/gRPC API (.NET 8+)
- **Deployment:** Backend jako serwis, frontend jako aplikacja desktopowa/webowa

### Faza 1: Refaktoryzacja Backend (ten dokument)
- **Frontend:** WinForms (istniejący, minimalnie modyfikowany)
- **Backend:** API-ready services (.NET 8 Web API)
- **Komunikacja:** In-process (temporary) → HTTP-ready
- **Czas:** 8-10 tygodni

### Faza 2: Nowy Frontend (przyszłość, poza scope'em tego dokumentu)
- Zastąpienie WinForms → Electron + React/Angular
- Backend już gotowy (z Fazy 1)
- Komunikacja: HTTP REST lub SignalR/WebSocket

---

## Problem: Obecny stan kodu

### Monolityczna architektura
```
┌─────────────────────────────────────────────────┐
│         FormGPS.cs (1287 linii)                 │
│                                                  │
│  ❌ UI (WinForms controls)                      │
│  ❌ Business Logic (guidance, field, sections)  │
│  ❌ Rendering (OpenGL)                          │
│  ❌ Hardware (GNSS, AutoSteer, IMU)             │
│  ❌ Configuration (Windows Registry)            │
│  ❌ File I/O (fields, boundaries, vehicles)     │
│                                                  │
│  Wszystko w jednym miejscu!                     │
└─────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────┐
│      GPS/Classes/ (44 klasy domenowe)           │
│                                                  │
│  ❌ 19 klas z "private readonly FormGPS mf;"    │
│  ❌ Tight coupling (mf.vehicle, mf.ABLine, ...) │
│  ❌ Logika mieszana z OpenGL rendering          │
│  ❌ Brak separacji concerns                     │
└─────────────────────────────────────────────────┘
```

### Konsekwencje obecnej architektury
- ❌ **Niemożliwa wymiana UI** - logika biznesowa w FormGPS
- ❌ **Trudne testowanie** - wszystko zależy od FormGPS
- ❌ **Brak możliwości API** - logika nie jest dostępna zewnętrznie
- ❌ **Tight coupling** - klasy domenowe zależą od FormGPS
- ❌ **Jeden wielki plik** - FormGPS 1287 linii

---

## Rozwiązanie: Strangler Fig Pattern

### Co to jest Strangler Fig?
Pattern migracji legacy systems, gdzie:
1. **Nowy kod** jest tworzony obok starego (nie zastępuje od razu)
2. **Stopniowo** przekierowujemy funkcjonalność do nowego kodu
3. **Aplikacja działa** przez cały czas migracji
4. **Stary kod** jest usuwany dopiero gdy nowy w 100% działa

Nazwa od rośliny "strangler fig" (dusiciel), która rośnie wokół drzewa i stopniowo je zastępuje.

### Etapy transformacji

#### Etap 0: Stan początkowy
```
┌─────────────────────────────────────────────────┐
│              FormGPS (WinForms)                  │
│         Wszystko w jednym miejscu               │
└─────────────────────────────────────────────────┘
```

#### Etap N (iteracja): Stan pośredni
```
┌─────────────────────────────────────────────────┐
│         FormGPS (WinForms - "cienki")           │
│  - UI + Rendering                               │
│  - API Client (wywołuje backend)                │
└──────────────────┬──────────────────────────────┘
                   │ In-process call
                   │ (później: HTTP)
                   ▼
┌─────────────────────────────────────────────────┐
│         Backend API (.NET 8)                    │
│  ✅ Część logiki już zmigrowana                 │
│  ✅ API Controllers                             │
│  ✅ Services (pure business logic)              │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
         ┌─────────────────────┐
         │   Legacy Code       │
         │   (stopniowo usuwany)│
         └─────────────────────┘
```

#### Etap końcowy (Faza 1 complete)
```
┌─────────────────────────────────────────────────┐
│         FormGPS (WinForms - "cienki")           │
│  - Tylko UI + Rendering (<300 linii)           │
│  - API Client                                   │
└──────────────────┬──────────────────────────────┘
                   │ In-process/HTTP
                   │ (łatwo przełączyć)
                   ▼
┌─────────────────────────────────────────────────┐
│         Backend API (.NET 8)                    │
│  ✅ 100% logiki biznesowej                      │
│  ✅ API Controllers (HTTP-ready)                │
│  ✅ Services                                    │
│  ✅ Models                                      │
│  ✅ Zero legacy code                            │
└─────────────────────────────────────────────────┘
```

---

## Kluczowa zasada: Zawsze działająca aplikacja

### Po każdym commit/merge:
✅ **Kod kompiluje się** bez błędów
✅ **FormGPS uruchamia się** poprawnie
✅ **Wszystkie funkcje działają** jak wcześniej
✅ **Testy przechodzą** (unit + integration + manual)
✅ **Zero regresji** w funkcjonalności

### Jeśli coś się psuje:
❌ **ROLLBACK** do poprzedniego działającego stanu
❌ **FIX** problemu przed merge
❌ **NIE mergować** broken code

### Strategie bezpieczeństwa:
1. **Feature Flags** - przełączanie między legacy/new code
2. **Adapter Pattern** - legacy code deleguje do new code
3. **Incremental rollout** - jeden moduł na raz
4. **Testing po każdym kroku** - unit + integration + manual

---

## Architektura docelowa (Faza 1)

### Backend API (.NET 8)
```
AgOpenGPS.Api/
├── Controllers/        # REST API endpoints (HTTP-ready)
├── Services/           # Business logic (pure, no UI)
├── Models/
│   ├── Domain/         # Domain models (pure business logic)
│   └── Dto/            # API contracts (JSON)
├── Infrastructure/
│   ├── EventBus/       # Pub/Sub (later: SignalR)
│   └── Persistence/    # JSON files, SQLite
└── Program.cs          # ASP.NET Core startup
```

### API Client (.NET Standard 2.0)
```
AgOpenGPS.Api.Client/
├── ApiClient.cs            # Main client interface
├── InProcess/              # In-process implementation (Faza 1)
│   └── InProcessClient.cs  # Direct service calls (no HTTP)
└── Http/                   # HTTP implementation (Faza 2)
    └── HttpClient.cs       # REST client (przyszłość)
```

### Frontend WinForms (.NET 4.8)
```
GPS/
├── Forms/
│   ├── FormGPS.cs          # Thin UI layer (<300 linii)
│   └── FormGPS_Rendering.cs # OpenGL rendering (uses API DTOs)
├── ApiClient/
│   └── BackendClient.cs    # Wrapper nad API Client
└── Legacy/                 # (stopniowo usuwany)
    └── Classes/            # 44 klasy (do refaktoryzacji)
```

---

## Roadmap (8-10 tygodni)

### Milestones przegląd

| # | Milestone | Czas | Co zostanie zmigrowane |
|---|-----------|------|------------------------|
| M0 | Infrastructure | 1 tydzień | Projekty Backend API + API Client |
| M1 | Configuration | 1 tydzień | Registry → JSON, pierwszy API endpoint |
| M2 | Guidance System | 2 tygodnie | ABLine, ABCurve, Guidance calculations |
| M3 | Field Management | 1 tydzień | Field, Boundary, Headland |
| M4 | Vehicle & Sections | 1 tydzień | Vehicle state, Sections control |
| M5 | Hardware | 1 tydzień | GNSS, IMU, AutoSteer |
| M6 | Rendering Data | 1 tydzień | Backend dostarcza dane do OpenGL |
| M7 | Legacy Cleanup | 1 tydzień | Usunięcie starego kodu |
| M8 | HTTP Ready | 1-2 tygodnie | Opcjonalnie: switch na HTTP |

**Szczegóły:** Zobacz [phase1-milestones.md](phase1-milestones.md)

---

## Success Metrics

### Techniczne
- ✅ Backend API (.NET 8) z 100% logiki biznesowej
- ✅ Zero dependencies Backend → WinForms
- ✅ FormGPS <300 linii (tylko UI + API calls)
- ✅ API Controllers gotowe na HTTP (tylko swap client implementation)
- ✅ Zero legacy code w GPS/Classes/
- ✅ Code coverage >70% dla Backend
- ✅ Performance ±5% vs legacy (nie wolniejsze)

### Funkcjonalne
- ✅ Aplikacja działa identycznie jak wcześniej
- ✅ Zero regresji w funkcjonalności
- ✅ Wszystkie features działają (guidance, field, sections, hardware)
- ✅ Konfiguracja automatycznie migrowana (Registry → JSON)
- ✅ Backwards compatibility (istniejące pliki użytkownika działają)

### Architektoniczne (gotowość na Fazę 2)
- ✅ Backend może działać standalone (Web API server)
- ✅ Frontend może być zastąpiony (Electron/React w Fazie 2)
- ✅ API DTOs zdefiniowane (clear contracts)
- ✅ API dokumentacja (Swagger/OpenAPI)
- ✅ Authentication/Authorization hooks ready (jeśli potrzebne w przyszłości)

---

## Korzyści z Fazy 1

### Immediate (podczas migracji)
1. **Lepszy kod** - separacja concerns, testability
2. **Łatwiejszy maintenance** - jasne granice odpowiedzialności
3. **Zespołowe** - można pracować równolegle (API + UI)

### Po Fazie 1
1. **API gotowe** - można budować nowe UI (web, mobile, CLI)
2. **Testowalne** - backend testowany bez UI
3. **Skalowalne** - backend może być osobnym serwisem
4. **Modern stack** - .NET 8, ASP.NET Core

### Długoterminowe (Faza 2 i dalej)
1. **Nowy frontend** - Electron + React/Angular
2. **Cloud-ready** - backend może być w chmurze
3. **Multi-platform** - backend Linux/macOS/Windows
4. **Integration** - inne aplikacje mogą używać API

---

## Ryzyka i Mitigacje

| Ryzyko | Prawdopodobieństwo | Wpływ | Mitigacja |
|--------|-------------------|-------|-----------|
| Breaking changes dla użytkowników | Niskie | Krytyczny | Backwards compatibility, migration utilities |
| Performance degradation | Średnie | Wysoki | Benchmarking po każdym milestone, profiling |
| Incomplete migration (legacy islands) | Średnie | Wysoki | Jasna definicja scope per milestone, tracking |
| Team capacity | Średnie | Średni | Priorytetyzacja, możliwość rozciągnięcia timeline |
| Bugs w refaktoryzowanym kodzie | Wysokie | Średni | Testy po każdym kroku, rollback strategy |

---

## Dokumenty powiązane

### High-level (ten folder)
- [phase1-api-architecture.md](phase1-api-architecture.md) - Szczegóły architektury API
- [phase1-migration-strategy.md](phase1-migration-strategy.md) - Jak migrować kod
- [phase1-milestones.md](phase1-milestones.md) - Lista milestones z opisem
- [phase1-technical-decisions.md](phase1-technical-decisions.md) - ADRs (Architecture Decision Records)

### Szczegółowe (tworzone per milestone)
- `milestones/m0-infrastructure.md` - Szczegółowy plan M0
- `milestones/m1-configuration.md` - Szczegółowy plan M1
- ... (tworzone gdy zaczynamy dany milestone)

### Refactoring guides (tworzone per klasa)
- `refactoring/CABLine-refactoring.md` - Jak zrefaktoryzować CABLine
- `refactoring/CBoundary-refactoring.md` - Jak zrefaktoryzować CBoundary
- ... (tworzone when needed)

---

## FAQ

### Dlaczego nie MVVM?
MVVM jest dobrym wzorcem dla aplikacji desktopowych, ale naszym celem końcowym jest Electron + React + API. API-first approach przygotowuje nas lepiej na Fazę 2.

### Dlaczego in-process w Fazie 1, a nie HTTP od razu?
In-process jest prostsze, szybsze, łatwiejsze do debugowania. HTTP dodamy opcjonalnie w M8 gdy backend będzie w 100% gotowy. Architektura jest HTTP-ready (Controllers, DTOs), tylko transport jest in-process.

### Co z AgIO?
AgIO będzie migrowane osobno, prawdopodobnie po Fazie 1 AgOpenGPS. Można też robić równolegle jeśli jest capacity.

### Czy FormGPS całkowicie zniknie?
Tak, ale dopiero w Fazie 2. W Fazie 1 FormGPS pozostaje (mocno "wychudzone"), ale nadal jest głównym UI.

### Co jeśli Faza 1 potrwa dłużej niż 10 tygodni?
To jest estimate. Możemy dostosować timeline w trakcie. Ważniejsze jest "zawsze działająca aplikacja" niż sztywny deadline.

---

## Następne kroki

1. ✅ **Teraz:** Przeczytać pozostałe high-level dokumenty
2. ⏳ **Potem:** Wybrać pierwszy milestone do implementacji (rekomendacja: M0)
3. ⏳ **Implementacja:** Utworzyć szczegółowy plan per milestone, implementować, testować
4. ⏳ **Iterate:** Powtórzyć dla kolejnych milestones

---

**Pytania? Zobacz [phase1-migration-strategy.md](phase1-migration-strategy.md) lub skontaktuj się z zespołem.**
