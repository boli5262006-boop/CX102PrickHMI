# Design

## Context

WPF HMI (.NET Framework 4.72, packages.config) on a single `MainViewModel` shared as DataContext by all pages. The OPC read helper `GetOPCUAValue` and its `Task.Run` caller in `ViewModels/MainViewModel.cs:415-490` are fully commented out, so `CurrentValue[VarName]` is never populated. The existing `updateTimer` 500 ms `UpdateTimer_Tick` (`MainViewModel.cs:111-114`) is empty. `thinger.CommunicationLib.OPCUA` provides `WriteNode<T>` / `WriteNodeAsync<T>`. Tag names/addresses live in `Settings/settings.json` as `GroupList[].VarList[]` (`VarName`, `VarAddress`, `CurrentValue` on each var). Manual `SetProperty` only — no `[ObservableProperty]` (source generators unavailable on net472/packages.config). Per-page ViewModels (`MonitorViewModel`, `AlarmListViewModel`, `HistoryAlarmViewModel`, `OverlapMonitorViewModel`) are dead code and must not be edited. Mock data today: simulation timer + `AdvanceSimulation`, `InitializeAlarmSeeds`/history seeds and fake `DateSummary`, hardcoded recipe/actual strings, mock Calibrate/Settings/Maintenance commands. SQLite Test insert exists in the startup path and must stay, commented.

## Goals / Non-Goals

**Goals:**
- All bound PLC values flow `CurrentValue[VarName]` → MainViewModel `SetProperty` notify properties → MonitorView XAML bindings (strict MVVM; color/text via XAML DataTriggers, no Brushes in the VM).
- Enable reads by uncommenting the poll loop only; implement writes as user-initiated async commands.
- Deliverables: `Behaviors/MomentaryButtonBehavior.cs`, `Views/NumericKeypadOverlay.xaml`/`.cs`, `Utilities/PlcTagLookup.cs`, `HoldButtonStyle` + `ReadonlyValueBoxStyle` in `styles/Controls.xaml`, and the MainViewModel/MonitorView changes.

**Non-Goals:**
- No changes to OPC poll delay/heartbeat/startup async, TIA Portal project, Overlap page, header `SystemStatus`, footer `FooterStatus`, 「报警 0 条」 row, dead per-page ViewModels, or AlarmList/HistoryAlarm item templates.
- No binding of the top 9 process-step lamps; no alarm pulse from fault tags (`IsAlarmActive` stays false).
- No new NuGet packages; no `[ObservableProperty]`; no business logic in `.xaml.cs`.

## Decisions

### D1: Snapshot pump pattern (500 ms `UpdateTimer_Tick` copies dictionary → properties)
Reuse the existing empty `updateTimer` tick. Each tick reads `CurrentValue[VarName]` for the 12 bound tags and calls `SetProperty` with `InvariantCulture` "F2" strings for numerics; bools pass through. Missing key ⇒ keep last value. While `KeypadTarget` matches an axis, skip overwriting that axis's `HomingRefPos*Text`.
- *Why not*: pushing `CurrentValue` dictionary straight into XAML (no per-tag change notification, breaks MVVM); parsing floats in the pump (string snapshot is sufficient since the VM writes floats itself).
- *Invariant*: read-only. The pump never calls `WriteNode*`.

### D2: Uncomment-only read pump
`GetOPCUAValue` and its `Task.Run` in `AttemptConnectionAndInitialData` are uncommented with zero edits inside the method body.
- *Why*: the method already implements the full read→`CurrentValue` store; rewriting risks timing/tag regressions and the plan locks "zero internal edits".

### D3: Momentary buttons via attached behavior (no NuGet, no code-behind)
`Behaviors/MomentaryButtonBehavior.cs` — DependencyProperty attached behavior on Button. `PreviewMouseLeftButtonDown` + `CaptureMouse` ⇒ invoke bound `ICommand` with `true`; `PreviewMouseLeftButtonUp` + `LostMouseCapture` ⇒ invoke with `false`. Touch works via WPF mouse promotion. The behavior only invokes the command; the VM command performs the PLC write. `HoldButtonStyle` (Surface2 `#232730` released / Primary `#0EA5E9` `IsPressed`) is a fresh style, distinct from `NavButtonStyle`/`MonitorNavButtonStyle`.
- *Why not*: `Microsoft.Xaml.Behaviors` package (new dependency, forbidden); `Click`/toggle (not momentary); code in `MonitorView.xaml.cs` (violates empty code-behind rule).

### D4: Write path — VarName lookup, then `WriteNodeAsync`
New `Utilities/PlcTagLookup.cs`: `TryGetAddress(device, varName, out address)` iterates all `GroupList.VarList` (ordinal ignore-case match) — no hardcoded group name. VM commands then `await CommonMethods.plc.WriteNodeAsync<bool>(address, value)` for `CmdHomingIn`/`CmdHomingOut`/`CmdSetup` and `WriteNodeAsync<float>(address, value)` for origin confirms. Failures (lookup miss or exception) set `CalibrationMessage`; no MessageBox.
- *Why not*: `WriteNode<T>` sync version (would block UI on comm timeout); caching addresses statically (settings can reload; lookup is cheap).

### D5: Single numeric keypad overlay with `KeypadTarget` axis state
`Views/NumericKeypadOverlay.xaml` (UserControl, hosted full-cell in MonitorView's root Grid; `Visibility` bound to `KeypadTarget != None`) with digits 0-9, decimal, backspace, 确认, 取消 — Buttons only (no system TextBox focus / TabTip). VM state: `KeypadTarget` (None/In/Out), `KeypadEditValue` buffer (F2 prefill), `HomingRefPosIn/OutInvalid` flags, `ConfirmKeypadCommand` (parse invariant `float`, must be > 0, else set invalid + `CalibrationMessage`; on success `WriteNodeAsync<float>` then update display text), `CancelKeypadCommand` (discard). Origin boxes use `ReadonlyValueBoxStyle` (`IsReadOnly=true`, `Focusable=false`) so taps route to the open-keypad commands.
- *Why not*: one keypad Window per axis (modal window churn, forbidden Window); per-axis overlays (duplicate XAML); free-typing in the box (defeats readonly + validation).

### D6: Mock removal strategy
Delete `_simulationTimer`/`AdvanceSimulation` and `InitializeSteps` seed content (keep `Steps` as empty `ObservableCollection` so MainWindow's `ItemsControl` renders an empty lamp row); empty `AlarmItems`/`HistoryItems` and blank `DateSummary` (properties kept — History/Alarm views still bind them); remove mock `CalibrateCommand`/`SettingsCommand`/`MaintenanceCommand`; initial `CalibrationMessage` = empty string. SQLite Test insert lines become comments (code preserved verbatim).
- *Why not*: deleting the alarm/history properties (would break `HistoryAlarmView.xaml` bindings); deleting the SQLite insert outright (plan requires it recoverable).

### D7: MonitorView binding details
Recipe/actual TextBlocks bind `RecipeRing1/2`, `ActualRing1/2`; fix the mislabeled outer actual (内侧刺针环 → 外侧刺针环, line ~114). Homing/enable lamps: `Fill` + text 已回原/未回原, 已使能/未使能 via DataTriggers on the bool properties (Success vs Ink3). Host row binds `MainMachineStop` (false ⇒ green 主机运行中, true ⇒ gray 主机已停机). Maintenance Button replaced by a `TriggerMaintenance` TextBlock (维修模式 激活/未激活, green vs Ink3). `d:DataContext` may stay design-time-only; do not edit `MonitorViewModel.cs`.

## Risks / Trade-offs

- [Uncommented poll loop floods UI updates every cycle] → Accept as-is: snapshot cadence is bounded by the existing timer-driven 500 ms copy; polling internals stay untouched per plan.
- [500 ms snapshot flickers numeric text on each tick] → `SetProperty` no-ops when the F2 string is unchanged, so XAML only re-renders on real changes.
- [Momentary write lost if mouse capture leaks (e.g., Alt+Tab mid-hold)] → `LostMouseCapture` handler also writes `false`; worst case a held PLC bit until next interaction, same risk class as physical hold buttons.
- [Origin write races a pump tick] → Pump skips the axis under edit; confirm updates text after the awaited write completes.
- [settings.json missing a required VarName] → Lookup failure surfaces in `CalibrationMessage`; read side keeps last value. Hardware round-trip verification is deferred to the user's PLC-connected QA pass.
- [net472 + packages.config] → No new packages; attached behavior hand-rolled; manual `SetProperty` only.
