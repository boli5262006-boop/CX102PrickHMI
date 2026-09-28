# Tasks

Reference: proposal.md (why/what), specs/monitor-plc-binding/spec.md (behavior contract), design.md (D1-D7 decisions).

## 1. OpenSpec Artifacts (Plan Task 1)

- [x] 1.1 Write `proposal.md`, `specs/monitor-plc-binding/spec.md`, `design.md`, `tasks.md` from `npx --yes @fission-ai/openspec instructions <artifact> --change bind-monitor-plc-points --json` templates; spec must cover the notify-property contract, momentary press/release, keypad >0 F2 confirm-only writes, MainMachineStop polarity, uncomment-only poll, empty alarm/history, commented SQLite Test; verify with `npx --yes @fission-ai/openspec status --change bind-monitor-plc-points` (all planning artifacts done; evidence: `.omo/evidence/task-1-openspec-status.txt`). Do NOT implement C#/XAML in this task; do NOT run `openspec init`.

## 2. MomentaryButtonBehavior (Plan Task 2)

- [ ] 2.1 Create `CX102PrickHMI/Behaviors/MomentaryButtonBehavior.cs` as an attached behavior on Button (DependencyProperty, no extra NuGet): `PreviewMouseLeftButtonDown` + CaptureMouse invokes bound ICommand with `true`; `PreviewMouseLeftButtonUp` + `LostMouseCapture` invokes with `false`; no OPC writes in the behavior; add Compile Include to csproj if non-SDK. Verify: grep file for `PreviewMouseLeftButtonDown`, `PreviewMouseLeftButtonUp`, `LostMouseCapture` (evidence: `.omo/evidence/task-2-behavior-grep.txt`).

## 3. NumericKeypadOverlay (Plan Task 3)

- [ ] 3.1 Create `Views/NumericKeypadOverlay.xaml` + `.xaml.cs` (InitializeComponent only): dark overlay using PageBackground/Surface/Primary colors, keys 0-9 + decimal + backspace + 确认 + 取消 as Buttons only (no system TextBox focus), bound to MainViewModel `KeypadEditValue`, `ConfirmKeypadCommand`, `CancelKeypadCommand`, `KeypadTarget` (Visibility when not None); no write logic in code-behind; 2-decimal display convention; add to csproj Page/Compile if explicit. Verify: grep XAML for 确认, 取消, Content="0".."9" (evidence: `.omo/evidence/task-3-keypad-keys.txt`).

## 4. Hold Button + Readonly Input Styles (Plan Task 4)

- [ ] 4.1 In `styles/Controls.xaml` add `HoldButtonStyle` (TargetType Button): released Background Surface2 #232730 / Foreground Ink / Border Border; `IsPressed` Background Primary #0EA5E9 / Foreground White — distinct from NavButtonStyle/MonitorButtonStyle; and `ReadonlyValueBoxStyle` based on InputStyle with `IsReadOnly=true`, `Focusable=false` (blocks TabTip), optional red-border DataTrigger for invalid origin. Verify: grep Controls.xaml for `x:Key="HoldButtonStyle"` and `x:Key="ReadonlyValueBoxStyle"`; HoldButtonStyle contains `IsPressed` and Primary/#0EA5E9 (evidence: `.omo/evidence/task-4-styles.txt`).

## 5. PlcTagLookup Helper (Plan Task 5)

- [ ] 5.1 Create `Utilities/PlcTagLookup.cs` with `TryGetAddress(OPCUADevice device, string varName, out string address)` iterating all `GroupList.VarList`, ordinal ignore-case match on VarName, returning VarAddress; null-safe; no static cache; no hardcoded group name (e.g., "Alarm"); no PLC writes here; add to csproj Compile if needed. Verify: grep file for `GroupList`, `VarAddress`, `VarName` and confirm no hardcoded "Alarm" filter (evidence: `.omo/evidence/task-5-lookup-grep.txt`).

## 6. MainViewModel: Properties, Pump, Writes, Uncomment Poll, Strip Mocks (Plan Task 6)

- [ ] 6.1 Uncomment `GetOPCUAValue` and its `Task.Run` call in `AttemptConnectionAndInitialData` with ZERO edits inside the method body. Verify: grep MainViewModel.cs for uncommented `private async Task GetOPCUAValue`; `dotnet build CX102PrickHMI.sln -c Debug` passes (evidence: `.omo/evidence/task-6-build.txt`).
- [ ] 6.2 Implement the notify-property contract (manual `SetProperty`, CommunityToolkit ObservableObject, no `[ObservableProperty]`): RecipeRing1/2, ActualRing1/2 (string F2), HomingRefPosInText/OutText (string F2), StsHomingDoneIn/Out, DriveInEnable/OutEnable, MainMachineStop, TriggerMaintenance (bool), IsAlarmActive (always false), CalibrationMessage (initial empty string), KeypadTarget (None/In/Out), KeypadEditValue, HomingRefPosInInvalid/OutInvalid — InvariantCulture F2 formatting; missing CurrentValue key keeps last value. Verify: grep MainViewModel.cs for each property name with `SetProperty` (evidence: `.omo/evidence/task-6-build.txt`).
- [ ] 6.3 Implement `UpdateTimer_Tick` snapshot: copy `CurrentValue[VarName]` → contract properties (reads only, MUST NOT call WriteNode/WriteNodeAsync); skip overwriting `HomingRefPos*Text` while `KeypadTarget` matches that axis. Verify: grep Tick body shows no Write* calls (evidence: `.omo/evidence/task-6-pump-no-write.txt`).
- [ ] 6.4 Implement commands: HoldHomingIn/Out/Setup (bool parameter from momentary behavior → PlcTagLookup → `await WriteNodeAsync<bool>`), OpenKeypadIn/Out (set KeypadTarget + F2 prefill), ConfirmKeypad (parse invariant float, require > 0 with 2-decimal formatting, else flag invalid + CalibrationMessage; on success `WriteNodeAsync<float>` then update display text), CancelKeypad (discard). Write/lookup failures set CalibrationMessage; no MessageBox. Verify: grep for `WriteNodeAsync` used in commands only (evidence: `.omo/evidence/task-6-build.txt`).
- [ ] 6.5 Strip mocks: delete `_simulationTimer`/`AdvanceSimulation` and `InitializeSteps` seed content (keep `Steps` as empty ObservableCollection), empty `AlarmItems`/`HistoryItems`, blank `DateSummary`, comment out (not delete) the SQLite Test insert, remove mock `CalibrateCommand`/`SettingsCommand`/`MaintenanceCommand`. Verify: grep MainViewModel.cs — no `模拟数据`/`125.00`/`198.50` active strings, no `_simulationTimer`, no new `AlarmEntry(` seeds (evidence: `.omo/evidence/task-6-build.txt`).

## 7. MonitorView Real Bindings (Plan Task 7)

- [ ] 7.1 Bind MonitorView.xaml: recipe/actual TextBlocks → RecipeRing1/2, ActualRing1/2 (fix label 内侧刺针环 → 外侧刺针环 on the outer actual column); replace 校准/设置/维修模式 buttons with three HoldButtonStyle buttons 校准内侧, 校准外侧, 设置 in one horizontal StackPanel using MomentaryButtonBehavior + HoldHomingIn/Out/Setup; replace maintenance Button with TriggerMaintenance TextBlock 维修模式 激活/未激活 (DataTrigger green Success vs Ink3); homing lamps StsHomingDoneIn/Out (已回原/未回原), enable rows DriveInEnable/OutEnable (已使能/未使能), host row MainMachineStop (false ⇒ green 主机运行中, true ⇒ gray 主机已停机); origin boxes ReadonlyValueBoxStyle bound HomingRefPosInText/OutText with OpenKeypadIn/Out commands and red-border invalid triggers; host NumericKeypadOverlay full-cell in the root Grid; bind CalibrationMessage to the existing TextBlock; title origin inputs 内侧伺服 校准原点值 / 外侧伺服 校准原点值. Verify: grep MonitorView.xaml for RecipeRing1, ActualRing1, StsHomingDoneIn, DriveInEnable, MainMachineStop, TriggerMaintenance, HomingRefPosInText, HoldHomingIn, 外侧刺针环; zero Buttons with Content 维修模式; MonitorView.xaml.cs still only InitializeComponent; build passes (evidence: `.omo/evidence/task-7-monitor-bindings.txt`).

## 8. Empty Alarm/History Seeds + Unbind Process-Step Animation (Plan Task 8)

- [ ] 8.1 Confirm alarm/history properties remain but construct empty (no seed AlarmEntry), `DateSummary` empty string (HistoryAlarmView keeps binding it), `Steps` empty with MainWindow ItemsControl left bound (no StsStep* PLC tags bound), no leftover simulation timer, AlarmListView/HistoryAlarmView item templates untouched. Verify: grep MainViewModel.cs for 伺服1 回原超时 / 125.00 / `_simulationTimer` — all absent (evidence: `.omo/evidence/task-8-no-seeds.txt`).

## Final Verification (Plan Tasks F1–F4)

- [ ] F1. Plan Compliance Audit (oracle): read the plan end-to-end; verify every Must Have exists and every Must NOT Have is absent from the codebase; check evidence under `.omo/evidence/`. Output `Must Have [N/N] | Must NOT Have [N/N] | Tasks [N/N] | VERDICT: APPROVE/REJECT`.
- [ ] F2. Code Quality Review: `dotnet build CX102PrickHMI.sln -c Debug` green; changed C# clean (no empty catches, no [ObservableProperty], no Brushes in VM); only permitted commented-out block is the SQLite Test insert. Output `Build [PASS/FAIL] | Files [N clean/N issues] | VERDICT`.
- [ ] F3. Real Manual QA: verify all Task 7 XAML bindings exist; grep Monitor path for 125.00 / 198.50 / 模拟数据 — none; GetOPCUAValue uncommented. Hardware PLC round-trip is a user-side check after handoff. Output `Scenarios [N/N pass] | VERDICT`.
- [ ] F4. Scope Fidelity Check (deep): diff vs plan — no OPC loop timing changes, no TIA Portal edits, no process-step PLC binding, no dead-VM edits, no new NuGet. Output `Tasks [N/N compliant] | Contamination [CLEAN/N issues] | VERDICT`.
