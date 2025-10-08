# Faza 1 – przegląd migracji

## Cel
- Zapewnienie wspólnego zrozumienia priorytetów i ryzyk podczas przejścia AgOpenGPS na architekturę backendu niezależną od platformy.
- Wypracowanie planu minimalizującego zależności od komponentów WinForms i rejestru Windows w głównych modułach aplikacji.

## Zakres
- Analiza istniejących modułów interfejsu użytkownika oraz warstwy konfiguracji pod kątem ścisłych powiązań z platformą Windows.
- Dokumentowanie potrzeb refaktoryzacji i stopniowe wyodrębnianie logiki biznesowej do warstw możliwych do ponownego użycia.
- Koordynacja prac z kolejnymi dokumentami fazy 1 opisującymi szczegóły migracji poszczególnych komponentów (np. `phase1-ui-decoupling.md`, `phase1-configuration-storage.md`).

## Aktualny stan
- `SourceCode/GPS/Forms/FormGPS.cs` zawiera rozbudowaną logikę sterującą powiązaną bezpośrednio z WinForms (`System.Windows.Forms`) i funkcjami natywnymi (`User32.dll`), a także integruje OpenGL przez `OpenTK`. Taka mieszanka zależności sprawia, że logika aplikacji jest trudna do wydzielenia poza proces okienkowy Windows, co uniemożliwia uruchomienie backendu w środowiskach Linux/macOS bez pełnej emulacji GUI.【F:SourceCode/GPS/Forms/FormGPS.cs†L1-L106】
- `SourceCode/GPS/Properties/RegistrySettings.cs` wykorzystuje rejestr Windows (`Microsoft.Win32.Registry`) do przechowywania ustawień i ścieżek roboczych oraz używa `MessageBox` do raportowania błędów. To podejście blokuje wdrożenie backendu na platformach bez rejestru i wymusza zależność od WinForms nawet w logice konfiguracji.【F:SourceCode/GPS/Properties/RegistrySettings.cs†L1-L94】

## Ograniczenia
- Wymagane jest zachowanie kompatybilności danych i konfiguracji istniejących użytkowników podczas refaktoryzacji źródła ustawień.
- Pierwsza faza migracji nie obejmuje jeszcze pełnej wymiany interfejsu użytkownika; działania koncentrują się na umożliwieniu rozdzielenia backendu od warstwy UI.
- Dostępne zasoby zespołu są ograniczone, dlatego prace muszą być możliwe do wykonania iteracyjnie i równolegle z bieżącymi wydaniami.

## Główne kamienie milowe
1. Zatwierdzenie docelowej architektury backendu w dokumencie `phase1-architecture-baseline.md`.
2. Opracowanie planu rozdzielenia logiki WinForms od warstwy sterującej w `phase1-ui-decoupling.md`.
3. Przygotowanie strategii migracji ustawień i magazynu danych w `phase1-configuration-storage.md`.
4. Uzgodnienie harmonogramu testów regresyjnych i automatyzacji w `phase1-testing-strategy.md`.

## Planowane zadania
- Utworzenie interfejsów abstrahujących operacje GUI oraz zdarzenia użytkownika (szczegóły w `phase1-ui-decoupling.md`).
- Zaprojektowanie nowego magazynu konfiguracji opartego na plikach wieloplatformowych wraz z warstwą migracji danych (`phase1-configuration-storage.md`).
- Identyfikacja i modularizacja logiki zależnej od OpenGL/OpenTK pod backend renderujący (`phase1-rendering-plan.md`).
- Przygotowanie wytycznych dla testów jednostkowych i integracyjnych wspierających backend niezależny od platformy (`phase1-testing-strategy.md`).

## Kryteria sukcesu
- Gotowe i zatwierdzone dokumenty szczegółowe fazy 1 (architektura, UI, konfiguracja, rendering, testy) wraz z planem implementacji.
- Zdefiniowany zestaw interfejsów i adapterów, który pozwala uruchomić podstawowe scenariusze backendowe bez bezpośredniego ładowania WinForms.
- Udokumentowana ścieżka migracji ustawień, która umożliwia przeniesienie istniejących danych użytkownika do rozwiązania niezależnego od Windows.
