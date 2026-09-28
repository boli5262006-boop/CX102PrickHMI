# WPF Independent Page Views Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the CX102PrickHMI WPF UI so the three HMI pages are independently editable XAML views while preserving the shared shell, MVVM navigation, mock data, ItemsControl collections, and the DingTalk JinBuTi font.

**Architecture:** `MainWindow.xaml` remains the shared HMI shell and binds `CurrentPage` to a `ContentControl`. Each page gets its own `UserControl` under `Views/`, with explicit `DataContext` supplied by the selected page ViewModel through WPF `DataTemplate` mappings kept only as lightweight View-to-ViewModel selectors. Page data and commands move into separate ViewModel files; simple row/step records move into `Models/`. Shared colors, font registration, and control styles move from `App.xaml` into `styles/Colors.xaml`, `styles/Controls.xaml`, and `styles/Generic.xaml`.

**Tech Stack:** .NET Framework 4.7.2, WPF, XAML, CommunityToolkit.Mvvm 8.4.2, old-style MSBuild project file, `ItemsControl` for process and alarm collections, Pack URI for the bundled TTF and PNG resources.

---

## File Map

### Create
- `CX102PrickHMI/Models/AlarmEntry.cs` — immutable display record for active and historical alarm rows.
- `CX102PrickHMI/Models/ProcessStep.cs` — immutable display record plus process-step state enum.
- `CX102PrickHMI/ViewModels/MonitorViewModel.cs` — monitor mock values, process steps, and monitor commands.
- `CX102PrickHMI/ViewModels/AlarmListViewModel.cs` — active alarm mock collection.
- `CX102PrickHMI/ViewModels/HistoryAlarmViewModel.cs` — historical alarm mock collection and filter display state.
- `CX102PrickHMI/Views/MonitorView.xaml` — independently editable monitor page body.
- `CX102PrickHMI/Views/MonitorView.xaml.cs` — generated UserControl initialization only.
- `CX102PrickHMI/Views/AlarmListView.xaml` — independently editable active-alarm page body.
- `CX102PrickHMI/Views/AlarmListView.xaml.cs` — generated UserControl initialization only.
- `CX102PrickHMI/Views/HistoryAlarmView.xaml` — independently editable history page body.
- `CX102PrickHMI/Views/HistoryAlarmView.xaml.cs` — generated UserControl initialization only.
- `CX102PrickHMI/styles/Colors.xaml` — shared brushes and font resource.
- `CX102PrickHMI/styles/Controls.xaml` — shared panel, navigation, button, input, table, and status styles.
- `CX102PrickHMI/styles/Generic.xaml` — merged-resource entry point for the two style dictionaries.

### Modify
- `CX102PrickHMI/App.xaml` — replace inline page DataTemplates and brushes/styles with one `styles/Generic.xaml` merged dictionary plus lightweight ViewModel-to-View DataTemplates.
- `CX102PrickHMI/MainWindow.xaml` — retain only header, navigation, monitor process-flow strip, ContentControl, and footer; use shared resources and the registered font.
- `CX102PrickHMI/ViewModels/MainViewModel.cs` — retain navigation and shell state; remove nested page ViewModels, nested models, and page mock-data factory implementations; instantiate the three standalone page ViewModels.
- `CX102PrickHMI/CX102PrickHMI.csproj` — explicitly include all new C#, XAML, dictionaries, font, and existing image resources as `Compile`, `Page`, `Resource`, or `None` items.

### Preserve
- `CX102PrickHMI/Assets/spike-roller-device.png` — monitor-page image resource.
- `CX102PrickHMI/Fonts/DingTalk-JinBuTi.ttf` — global application font resource.
- `CX102PrickHMI/MainWindow.xaml.cs` — initialization-only code-behind.

---

### Task 1: Split display models from the main ViewModel

**Files:**
- Create: `CX102PrickHMI/Models/AlarmEntry.cs`
- Create: `CX102PrickHMI/Models/ProcessStep.cs`
- Create: `CX102PrickHMI/ViewModels/MonitorViewModel.cs`
- Create: `CX102PrickHMI/ViewModels/AlarmListViewModel.cs`
- Create: `CX102PrickHMI/ViewModels/HistoryAlarmViewModel.cs`
- Modify: `CX102PrickHMI/ViewModels/MainViewModel.cs`

- [ ] **Step 1: Add the model files with only display state.**

  `AlarmEntry` must expose `Time`, `Message`, `Level`, and `RecoveryTime` as get-only strings. `ProcessStep` must expose `Title`, `Status`, `State`, and `Number`; define `ProcessStepState` with `Pending`, `Completed`, and `Error` values. These models must contain no service, persistence, or device code.

- [ ] **Step 2: Move monitor-specific data and commands to `MonitorViewModel`.**

  Keep the existing mock values exactly: recipe `125.00`/`198.50`, actual `124.98`/`198.52`, servo values `0.00`, and the nine process steps with the existing statuses. Keep `CalibrateCommand`, `SettingsCommand`, and `MaintenanceCommand`; each command only updates the display-only `CalibrationMessage`.

- [ ] **Step 3: Move active and historical collections to their own ViewModels.**

  `AlarmListViewModel` owns the six active rows. `HistoryAlarmViewModel` owns the eight historical rows and exposes `DateSummary`. Both continue to use `ObservableCollection<AlarmEntry>` so the views can bind through `ItemsControl`.

- [ ] **Step 4: Reduce `MainViewModel` to shell state and navigation.**

  `MainViewModel` owns `Monitor`, `Alarms`, `History`, `CurrentPage`, `ShowMonitorCommand`, `ShowAlarmsCommand`, and `ShowHistoryCommand`, plus `SystemStatus`, `FooterStatus`, and `Version`. It must construct each standalone ViewModel directly and must not contain nested model or page ViewModel declarations.

- [ ] **Step 5: Compile the C# portion before adding new XAML.**

  Run:

  ```powershell
  dotnet build "CX102PrickHMI.sln" --no-restore
  ```

  Expected result: no C# errors. XAML errors caused by the still-existing page templates are acceptable at this intermediate step only if the final build is rerun after Task 4.

---

### Task 2: Create shared style dictionaries and register the font

**Files:**
- Create: `CX102PrickHMI/styles/Colors.xaml`
- Create: `CX102PrickHMI/styles/Controls.xaml`
- Create: `CX102PrickHMI/styles/Generic.xaml`
- Modify: `CX102PrickHMI/CX102PrickHMI.csproj`

- [ ] **Step 1: Put all shared brushes and the font family in `Colors.xaml`.**

  Preserve the HTML palette resources under these keys: `PageBackground`, `Surface`, `Surface2`, `Line`, `Border`, `Primary`, `PrimaryDark`, `Ink`, `Ink2`, `Ink3`, `Success`, `Warning`, `Error`, and `Info`. Add:

  ```xml
  <FontFamily x:Key="AppFont">/CX102PrickHMI;component/Fonts/#钉钉进步体</FontFamily>
  ```

  Use the actual bundled file `CX102PrickHMI/Fonts/DingTalk-JinBuTi.ttf`; do not introduce a new font filename or external download.

- [ ] **Step 2: Move reusable control styles to `Controls.xaml`.**

  Define `PanelStyle`, `NavButtonStyle`, `PrimaryButtonStyle`, `SecondaryButtonStyle`, and `InputStyle`. Add shared default typography styles for `TextBlock`, `Button`, `TextBox`, `ComboBox`, and `ListView`/table text where the default style will not break control templates. Set the default application font to `{StaticResource AppFont}` while leaving numeric/time fields explicitly on `Consolas`.

- [ ] **Step 3: Make `Generic.xaml` the only style merge entry point.**

  `Generic.xaml` must merge `Colors.xaml` first and `Controls.xaml` second using component Pack URIs:

  ```xml
  <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="/CX102PrickHMI;component/styles/Colors.xaml" />
      <ResourceDictionary Source="/CX102PrickHMI;component/styles/Controls.xaml" />
  </ResourceDictionary.MergedDictionaries>
  ```

- [ ] **Step 4: Include dictionaries and font as WPF resources.**

  Add explicit old-style project entries:

  ```xml
  <Page Include="styles\Colors.xaml" />
  <Page Include="styles\Controls.xaml" />
  <Page Include="styles\Generic.xaml" />
  <Resource Include="Fonts\DingTalk-JinBuTi.ttf" />
  ```

  Do not rely on SDK-style wildcard inclusion.

---

### Task 3: Create independent page XAML views

**Files:**
- Create: `CX102PrickHMI/Views/MonitorView.xaml`
- Create: `CX102PrickHMI/Views/MonitorView.xaml.cs`
- Create: `CX102PrickHMI/Views/AlarmListView.xaml`
- Create: `CX102PrickHMI/Views/AlarmListView.xaml.cs`
- Create: `CX102PrickHMI/Views/HistoryAlarmView.xaml`
- Create: `CX102PrickHMI/Views/HistoryAlarmView.xaml.cs`

- [ ] **Step 1: Create `MonitorView.xaml` as a focused UserControl.**

  Bind its root `DataContext` to `MonitorViewModel` using `d:DataContext` for design-time support. Use a two-column root Grid with a fixed proportional split matching the HTML: left content `2*`, right device state `1*`. Use nested equal-width two-column Grids for recipe/actual KPI cards and servo input/status rows. Keep the device image at `Height="180"` with `Stretch="Uniform"` and source `/CX102PrickHMI;component/Assets/spike-roller-device.png`. Bind the three commands and all monitor display properties with `Mode=OneWay` for read-only values.

- [ ] **Step 2: Render monitor process steps with `ItemsControl`.**

  The page or shell may render the process strip, but the collection must remain `ItemsControl`-based. Each item must display number, title, and status, with visual state driven by `ProcessStepState` through styles/triggers or a small converter; do not add code-behind event handlers.

- [ ] **Step 3: Create `AlarmListView.xaml` as an independent table view.**

  Use a header Grid and an `ItemsControl ItemsSource="{Binding Items}"`. Give header and row Grids identical column definitions so `报警时间`, `报警内容`, and `级别` align vertically. Keep the time column on `Consolas`; use `AppFont` for Chinese labels and content. Keep the footer count text in this view.

- [ ] **Step 4: Create `HistoryAlarmView.xaml` as an independent filter/table view.**

  Use a dedicated filter Grid/StackPanel with date fields, level ComboBox, query button, and export button. Use a header Grid and row Grid with identical four-column definitions for time, message, level, and recovery time. Keep all eight mock rows and the date summary bound from `HistoryAlarmViewModel`.

- [ ] **Step 5: Keep all page code-behind initialization-only.**

  Each `.xaml.cs` file must contain only the generated `UserControl` partial class and constructor calling `InitializeComponent()`. No click handlers, data creation, layout logic, or service calls are permitted.

---

### Task 4: Simplify App resources and convert MainWindow to the shared shell

**Files:**
- Modify: `CX102PrickHMI/App.xaml`
- Modify: `CX102PrickHMI/MainWindow.xaml`
- Preserve: `CX102PrickHMI/MainWindow.xaml.cs`

- [ ] **Step 1: Replace inline App resources with a merged dictionary.**

  `App.xaml` must contain only the `Generic.xaml` merge and lightweight DataTemplates that map each page ViewModel type to its independent View type, for example:

  ```xml
  <DataTemplate DataType="{x:Type vm:MonitorViewModel}">
      <views:MonitorView />
  </DataTemplate>
  ```

  Add `xmlns:views="clr-namespace:CX102PrickHMI.Views"` and remove all page-body markup from `App.xaml`.

- [ ] **Step 2: Keep `MainWindow.xaml` limited to the shell.**

  Preserve the header, navigation buttons, process-flow panel, `ContentControl Content="{Binding CurrentPage}"`, and footer. Remove all monitor KPI, control, device, active alarm, and history table markup from the window. Apply `{StaticResource AppFont}` at the window/root level, while preserving `Consolas` on time and numeric values.

- [ ] **Step 3: Make navigation visuals reflect the current page without code-behind.**

  Bind each navigation button command to the corresponding command. Use a ViewModel page-key or style trigger pattern for selected state; do not hardcode the monitor button as always active. If the existing navigation scope is kept minimal, use separate selected-state properties in `MainViewModel` and `DataTrigger`s in `NavButtonStyle`.

- [ ] **Step 4: Verify no page body remains in App or MainWindow.**

  Search the files for page-specific labels and ensure each appears only in its own View except shared navigation labels. The three page-specific titles and table content must be editable in their respective XAML files.

---

### Task 5: Wire all new files into the old-style project

**Files:**
- Modify: `CX102PrickHMI/CX102PrickHMI.csproj`

- [ ] **Step 1: Add explicit Compile/Page/Resource entries.**

  Add `Compile` items for all files under `Models`, `ViewModels`, and `Views/*.xaml.cs`; add `Page` items for all `Views/*.xaml` and `styles/*.xaml`; keep `Resource` entries for `Assets/spike-roller-device.png` and `Fonts/DingTalk-JinBuTi.ttf`.

- [ ] **Step 2: Remove obsolete compile inclusion.**

  Remove the old `ViewModels\MainViewModel.cs` entry only if it is replaced by a new file path; do not create duplicate class includes. Keep existing App/MainWindow generated item metadata consistent with the old project format.

- [ ] **Step 3: Build the complete project.**

  Run:

  ```powershell
  dotnet build "CX102PrickHMI.sln" --no-restore
  ```

  Expected result: `Build succeeded`, `0 Error`; any warning must be investigated and either removed or reported as pre-existing.

---

### Task 6: Verify startup, page navigation, bindings, and layout structure

**Files:**
- Verify: `CX102PrickHMI/bin/Debug/CX102PrickHMI.exe`
- Verify: `CX102PrickHMI/App.xaml`, `CX102PrickHMI/MainWindow.xaml`, `CX102PrickHMI/Views/*.xaml`, `CX102PrickHMI/styles/*.xaml`

- [ ] **Step 1: Launch the actual executable.**

  Run the built EXE with PowerShell, keep it alive for at least five seconds, and capture redirected standard output/error. Expected result: process remains running and no `XamlParseException` appears.

- [ ] **Step 2: Exercise all three navigation commands.**

  Use a WPF-capable UI automation/manual desktop check to activate `刺辊监控`, `报警记录`, and `历史报警` in sequence. Expected result: each independent UserControl appears in the same shell without closing or throwing binding exceptions.

- [ ] **Step 3: Check the known alignment-sensitive regions.**

  Confirm visually that monitor KPI cards, servo fields, device panel, alarm table headers/rows, and history table headers/rows use matching Grid columns and consistent margins. Confirm the DingTalk font is visible on Chinese UI text and numeric fields remain readable in `Consolas`.

- [ ] **Step 4: Check binding diagnostics.**

  Review the Visual Studio/WPF Output binding messages if available. There must be no errors for missing properties, invalid DataTemplates, failed Pack URIs, or attempts to TwoWay-bind read-only properties.

- [ ] **Step 5: Record test limitation accurately.**

  The repository currently has no test project. Do not claim `dotnet test` passed; report that no automated test project exists and that verification used build plus actual process launch and UI navigation.

---

## Self-Review Checklist

- [x] Each requirement has a task: independent XAML pages, shared shell, MVVM split, ItemsControl collections, styles dictionaries, font resource, mock-only scope, navigation, and build/runtime verification.
- [x] No backend/device integration is included.
- [x] No page body remains in `App.xaml` after Task 4; only ViewModel-to-View selectors remain.
- [x] All paths use the existing old-style project layout and the actual font filename `DingTalk-JinBuTi.ttf`.
- [x] Read-only display bindings explicitly use `Mode=OneWay` to prevent the previously observed startup `XamlParseException`.
- [x] Header and row tables use identical Grid column definitions to directly address the reported uneven alignment.
- [x] No placeholder steps or unspecified files remain.
