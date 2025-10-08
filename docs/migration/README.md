# Dokumentacja Migracji AgOpenGPS

## Faza 1: Backend API + WinForms Refactoring

### Dokumenty High-Level (aktywne)

Przeczytaj w tej kolejności:

1. **[phase1-strangler-fig-overview.md](phase1-strangler-fig-overview.md)**
   - Przegląd całej Fazy 1
   - Strangler Fig Pattern
   - Cel końcowy: Electron + React + API
   - Success metrics

2. **[phase1-api-architecture.md](phase1-api-architecture.md)**
   - Szczegóły architektury Backend API
   - Struktura projektów
   - Warstwy (Controllers, Services, Models, DTOs)
   - Data flow

3. **[phase1-migration-strategy.md](phase1-migration-strategy.md)**
   - JAK migrować kod
   - Adapter Pattern
   - Feature Flags
   - Testing strategy

4. **[phase1-milestones.md](phase1-milestones.md)**
   - Lista milestones (M0-M8)
   - Timeline (8-10 tygodni)
   - Dependencies
   - Scope per milestone

5. **[phase1-technical-decisions-api.md](phase1-technical-decisions-api.md)**
   - Architecture Decision Records (ADRs)
   - Uzasadnienia kluczowych decyzji
   - .NET 8, ASP.NET Core, DI, JSON config, etc.

### Dokumenty pomocnicze

- **[phase1-overview-critique.md](phase1-overview-critique.md)**
  - Krytyczna analiza pierwotnego planu
  - Identyfikacja problemów i rekomendacje
  - Zachowane jako reference

### Szczegółowe plany (tworzone per milestone)

Tworzone when needed, per milestone:
- `milestones/m0-infrastructure-detailed.md` (gdy zaczynamy M0)
- `milestones/m1-configuration-detailed.md` (gdy zaczynamy M1)
- ... (itd)

### Refactoring guides (tworzone per klasa)

Tworzone when needed, per klasa:
- `refactoring/CABLine-refactoring.md`
- `refactoring/CBoundary-refactoring.md`
- ... (itd)

## Archiwum

Folder [archive/](archive/) zawiera nieaktualne dokumenty (pierwotny plan MVVM, zastąpiony przez API-first).

## Następne kroki

1. ✅ Przeczytać wszystkie high-level dokumenty (powyżej)
2. ⏳ Wybrać milestone do rozpoczęcia (rekomendacja: M0)
3. ⏳ Utworzyć szczegółowy plan dla wybranego milestone
4. ⏳ Implementować, testować, review, merge
5. ⏳ Powtórzyć dla kolejnych milestones

## Status Fazy 1

- [ ] M0: Infrastructure Setup
- [ ] M1: Configuration API
- [ ] M2: Guidance System API
- [ ] M3: Field Management API
- [ ] M4: Vehicle & Sections API
- [ ] M5: Hardware Communication API
- [ ] M6: Rendering Data API
- [ ] M7: Legacy Code Cleanup
- [ ] M8: HTTP Ready (optional)

**Faza 1 ukończona gdy:** M0-M7 ✅

---

**Pytania?** Skontaktuj się z zespołem lub stwórz issue w repozytorium.
