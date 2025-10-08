# Migracja – Faza 1: Architektura domenowa

## Mapowanie klas na moduły domenowe

| Moduł domenowy | Przypisane klasy |
| --- | --- |
| **Nawigacja i planowanie przejazdów** | `CGuidance`, `CABLine`, `CABCurve`, `CDubins`, `CHead`, `CHeadLine`, `CTrack`, `CYouTurn`, `CContour`, `CTram`, `CTurn`, `CTurnLines`, `CRecordedPath` |
| **Zarządzanie polem i geometrią** | `CBoundary`, `CBoundaryList`, `CFieldData`, `CPolygon`, `CWorldGrid`, `CFence`, `CFenceLine`, `CFlag`, `CPatches` |
| **Sterowanie sekcjami i narzędziem** | `CSection`, `CTool`, `CFeatureSettings`, `CBrightness` |
| **Pojazd i integracja sprzętu** | `CVehicle`, `CAutoSteer`, `CAHRS`, `CNMEA`, `CModuleComm`, `CSound` |
| **Wizualizacja i grafika 3D** | `CCamera`, `CFont`, `CGLM`, `CContour` (rendering), `CTram` (rendering), `CFence` (rendering) |
| **Symulacja i testy** | `CSim` |
| **Funkcje pomocnicze i matematyka** | `CExtensionMethods`, `vec3` |

> **Uwaga:** Część klas (np. `CContour`, `CTram`, `CFence`) pełni równocześnie role obliczeniowe i renderujące. W architekturze warstwowej planowane jest wydzielenie logiki domenowej do serwisów, a renderingu do adapterów UI.

## Kontrakty modułów

### Nawigacja i planowanie przejazdów
- `IGuidanceService`
  - Zapewnia aktualne komendy prowadzenia (linia docelowa, odchyłka, status zawracania).
  - Zarządza planowaniem przejazdów na podstawie danych pola i stanu pojazdu.
- `IPathPlanner`
  - Generuje ścieżki AB, krzywe i zawracania.
  - Dostarcza podgląd geometryczny dla UI.
- `ITramlineService`
  - Oblicza i utrzymuje tramliny oraz ich status aktywacji.
- Zdarzenia/DTO publikowane przez backend
  - `GuidanceUpdatedEvent` (id ścieżki, odchyłka, kąt kursu, stan autopilota).
  - `PathPreviewDto` (lista punktów polilinii, typ ścieżki).
  - `TurnStateChangedEvent` (tryb zawracania, sekwencja manewru, czas do kolejnego etapu).

### Zarządzanie polem i geometrią
- `IFieldRepository`
  - Przechowuje i udostępnia dane pól, granic oraz zapisane ścieżki.
  - Wspiera wersjonowanie i metadane (nazwa, operator, data).
- `IBoundaryService`
  - Normalizuje i waliduje granice oraz strefy no-go.
  - Dostarcza poligony do planera i sekcji.
- `IHeadlandGenerator`
  - Tworzy linie nawrotów i strefy ochronne.
- Zdarzenia/DTO
  - `FieldLoadedEvent` (id pola, powierzchnia, jednostki, status synchronizacji).
  - `BoundaryChangedEvent` (poligony granic, strefy wewnętrzne, timestamp).
  - `FieldCatalogDto` (lista pól i ich parametrów dla selektora w UI).

### Sterowanie sekcjami i narzędziem
- `ISectionControlService`
  - Oblicza stany ON/OFF sekcji na podstawie GPS, map pokrycia i logiki opóźnień.
  - Generuje komendy do sterownika narzędzia.
- `IImplementConfigurationProvider`
  - Dostarcza konfigurację szerokości sekcji, opóźnień, parametrów jasności.
- `ICoverageMapService`
  - Udostępnia dane o pokryciu dla wizualizacji i analizy.
- Zdarzenia/DTO
  - `SectionStateChangedEvent` (lista sekcji z aktualnym stanem i powodem zmiany).
  - `CoverageUpdatedEvent` (aktualizacja mapy pokrycia dla kafli/poligonów).
  - `ImplementSettingsDto` (konfiguracja do edycji w UI).

### Pojazd i integracja sprzętu
- `IVehicleStateService`
  - Normalizuje dane pozycji, prędkości i orientacji pojazdu.
- `IAutoSteerGateway`
  - Komunikuje się z kontrolerem autosterowania (wysyłanie/odbiór ramek CAN/UDP).
- `IImuGateway`
  - Zapewnia dane z żyroskopu/akcelerometru.
- `IGnssGateway`
  - Dostarcza rozwiązania GNSS i metadane dokładności.
- `IHardwareDiagnosticsService`
  - Konsoliduje status modułów (komunikacja, dźwięk, alerty).
- Zdarzenia/DTO
  - `VehiclePoseUpdatedEvent` (pozycja, orientacja, prędkość, źródło).
  - `HardwareStatusEvent` (status modułów, napięcia, błędy).
  - `AutoSteerCommandDto` (komendy wysyłane do sterownika w trybie manualnym/testowym).

### Wizualizacja i grafika 3D
- `IRenderSceneProvider`
  - Udostępnia znormalizowane dane sceny (siatka, kamery, modele narzędzi).
- `ICameraStateService`
  - Zarządza parametrami kamery 3D/2D, powiązanymi z ruchem pojazdu.
- `IThemeService`
  - Definiuje palety kolorów, jasność, tryb dzienny/nocny.
- Zdarzenia/DTO
  - `SceneStateDto` (obiekty do narysowania, tekstury, poziomy szczegółowości).
  - `CameraChangedEvent` (pozycja, kierunek, zoom).
  - `DisplayModeChangedEvent` (tryb dzienny/nocny, jasność).

### Symulacja i testy
- `ISimulationService`
  - Udostępnia scenariusze symulacyjne dla testów i szkolenia.
- `ISimulatedHardwareGateway`
  - Emuluje GNSS/IMU/sekcje do testów offline.
- Zdarzenia/DTO
  - `SimulationScenarioLoadedEvent` (konfiguracja symulacji, tempo czasu).
  - `SimulatedDataFrameDto` (pakiety symulowanych danych).

### Funkcje pomocnicze i matematyka
- `IMathToolkit`
  - Zapewnia operacje wektorowe i macierzowe wspólne dla modułów.
- `IExtensionRegistry`
  - Rejestruje zestawy rozszerzeń (np. formatery, transformaty).
- Zdarzenia/DTO
  - Moduł pełni rolę biblioteki wspólnej – nie publikuje zdarzeń, ale udostępnia zestawy metod.

## Integracja z UI WinForms

### Strumień zdarzeń → kontrolki
- `GuidanceUpdatedEvent` → aktualizacja wskaźnika odchyłki, ikon autopilota oraz nakładki linii AB.
- `TurnStateChangedEvent` → panel zawracania (ikonografia etapów).
- `FieldLoadedEvent`/`BoundaryChangedEvent` → lista pól, mapa granic, aktualizacja stanu przycisków Start/Stop.
- `SectionStateChangedEvent`/`CoverageUpdatedEvent` → kontrolki sekcji (lampki LED, paski postępu), mapa pokrycia.
- `VehiclePoseUpdatedEvent` → wskaźnik prędkości, heading, model pojazdu.
- `HardwareStatusEvent` → panel diagnostyki, alerty dźwiękowe.
- `CameraChangedEvent`/`DisplayModeChangedEvent` → właściwości kontrolki renderującej OpenGL.

### Adaptery i warstwy pośrednie
1. **Adapter zdarzeń → WinForms (`WinFormsEventAdapter`)**
   - Subskrybuje zdarzenia domenowe i przekłada je na bezpieczne wątki UI (`SynchronizationContext`).
   - Aktualizuje modele widoku (np. `GuidanceViewModel`).
2. **Modele widoku**
   - Buforują ostatni stan DTO i udostępniają właściwości z możliwością powiadamiania (INotifyPropertyChanged).
3. **Adapter renderujący (`OpenGlSceneAdapter`)**
   - Tłumaczy `SceneStateDto` na obiekty OpenGL używane przez kontrolkę renderującą.
4. **Adapter wejścia użytkownika (`WinFormsCommandAdapter`)**
   - Przechwytuje interakcje użytkownika (przyciski, suwaki) i publikuje komendy do backendu (`ICommandBus`).
5. **Warstwa mapowania DTO (`DtoMapper`)**
   - Konwertuje modele domenowe na DTO przyjazne UI (np. formatowanie jednostek, kolory).

### Przepływ integracji
1. Backend publikuje zdarzenie (np. `GuidanceUpdatedEvent`).
2. `WinFormsEventAdapter` otrzymuje zdarzenie, marshaluje na główny wątek i aktualizuje odpowiedni model widoku.
3. Kontrolki WinForms są zbindowane do modeli widoku lub otrzymują powiadomienie o aktualizacji i odświeżają się.
4. Zdarzenia interakcji użytkownika (np. włączenie sekcji) przechodzą przez `WinFormsCommandAdapter`, który wywołuje odpowiedni kontrakt (np. `ISectionControlService`).

---
Dokument stanowi podstawę do separacji logiki domenowej od warstwy UI oraz przygotowania serwisów i kontraktów do dalszych faz migracji.
