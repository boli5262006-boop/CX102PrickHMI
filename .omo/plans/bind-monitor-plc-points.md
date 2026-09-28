# Bind Monitor Page to PLC Points

## TL;DR

> **Quick Summary**: Remove Monitor mock data, uncomment the existing OPC read pump as-is, snapshot `CurrentValue[VarName]` onto MainViewModel notify properties every 500ms, and wire MonitorView to real PLC tags (momentary commands, status lamps+text, numeric keypad for origin). OpenSpec change `bind-monitor-plc-points` is created first.
>
> **Deliverables**:
> - OpenSpec artifacts under `openspec/changes/bind-monitor-plc-points/`
> - MainViewModel PLC notify properties + snapshot pump + async writes
> - MonitorView real bindings, hold-button styles, numeric keypad overlay
> - Mock seeds emptied; SQLite Test insert commented
>
> **Estimated Effort**: Medium
> **Parallel Execution**: YES - 3 waves
> **Critical Path**: Task 1 → Task 5 → Task 6 → Task 7 → F1-F4

---

## Context

### Original Request
去掉模拟数据，用 settings.json 点位把 PLC 读值绑定到监控界面，点动写校准/设置，原点数字键盘，维修改状态显示。先写 OpenSpec 再改。用户确认 WPF MVVM、通知属性。

### Interview Summary
**Key Discussions**:
- Scope B: Monitor this round; process-step lamps unbound; alarm pulse off
- Momentary: press true / release or LostMouseCapture false
- Keypad overlay A, F2, > 0 only, confirm writes
- MainMachineStop false=green running, true=gray stopped
- Write: VarName → VarAddress → WriteNodeAsync
- Uncomment GetOPCUAValue as-is (it is currently commented; without it CurrentValue stays empty)

**Research Findings**:
- `OPCUA.WriteNode<T>` / `WriteNodeAsync<T>` exist
- Read stores `CurrentValue[VarName]`; poll loop commented at MainViewModel.cs:425-490
- Empty 500ms `UpdateTimer_Tick` at MainViewModel.cs:111-114
- All pages DataContext = MainViewModel; do not edit dead per-page ViewModels
- Manual `SetProperty` only — no `[ObservableProperty]` (net472 packages.config)

### Metis Review
**Identified Gaps** (addressed):
- Read pump disabled: uncomment only
- Alarm/history share MainViewModel: empty collections, keep properties
- Header/footer / 报警 0 条 out of scope
- Attached behavior for momentary (xaml.cs empty)
- WriteNodeAsync + CalibrationMessage on failure
- KeypadTarget enum + F2 prefill

---

## Work Objectives

### Core Objective
Bind the Monitor page to live PLC tags through MainViewModel notify properties fed from `CurrentValue[VarName]`, with momentary writes and a validated origin keypad.

### Concrete Deliverables
- `openspec/changes/bind-monitor-plc-points/{proposal,design,tasks}.md` and `specs/monitor-plc-binding/spec.md`
- `ViewModels/MainViewModel.cs` notify properties + pump + writes
- `Views/MonitorView.xaml` real bindings
- `Behaviors/MomentaryButtonBehavior.cs`
- `Views/NumericKeypadOverlay.xaml` + `.cs` (empty code-behind except InitializeComponent)
- `Utilities/PlcTagLookup.cs`
- Style keys in `styles/Controls.xaml`

### Definition of Done
- [ ] `dotnet build CX102PrickHMI.sln -c Debug` → 0 errors
- [ ] OpenSpec change artifacts exist and `npx @fission-ai/openspec status --change bind-monitor-plc-points` shows tasks done
- [ ] Monitor path has no hardcoded 125.00 / 198.50 / 模拟数据

### Must Have
- All bound PLC values are MainViewModel `SetProperty` notify properties (MVVM)
- XAML DataTriggers for lamp color+text; no Brushes in VM
- Press true / release false for CmdHomingIn, CmdHomingOut, CmdSetup
- Origin > 0, 2 decimals, keypad confirm writes float via VarAddress
- GetOPCUAValue uncommented with zero internal edits
- SQLite Test insert commented, not deleted

### Must NOT Have (Guardrails)
- Do not edit MonitorViewModel / OverlapMonitorViewModel / AlarmListViewModel / HistoryAlarmViewModel
- Do not retune OPC poll delay, heartbeat, or startup async
- Do not bind top 9 process-step lamps
- Do not enable device-image alarm pulse (IsAlarmActive always false)
- Do not add NuGet packages
- Do not use `[ObservableProperty]`
- Do not put business logic in `.xaml.cs` (attached behavior + VM methods only)
- Do not write from the 500ms pump
- Do not touch TIA Portal, Overlap page, header SystemStatus, footer FooterStatus, 「报警 0 条」

### Spec Framework Integration
- **Detected Framework**: OpenSpec
- **Config File**: `openspec/config.yaml`
- **Active Specs**: none yet
- **Active Changes/Proposals**: `bind-monitor-plc-points` (scaffold exists; artifacts must be written by executor)
- **Available Commands**: `npx --yes @fission-ai/openspec`
- **Spec-to-Task Mapping**: Task 1 writes artifacts; Tasks 2–8 implement `monitor-plc-binding`

### MVVM notify property contract (executor MUST implement all)

| VM property | Type | Source VarName | Direction |
|---|---|---|---|
| RecipeRing1 | string F2 | RcpActIn | read snapshot |
| RecipeRing2 | string F2 | RcpActOut | read snapshot |
| ActualRing1 | string F2 | ActPosIn | read snapshot |
| ActualRing2 | string F2 | ActPosOut | read snapshot |
| HomingRefPosInText | string F2 | HomingRefPosIn | display; keypad confirm writes |
| HomingRefPosOutText | string F2 | HomingRefPosOut | display; keypad confirm writes |
| StsHomingDoneIn | bool | StsHomingDoneIn | read |
| StsHomingDoneOut | bool | StsHomingDoneOut | read |
| DriveInEnable | bool | DriveInEnable | read |
| DriveOutEnable | bool | DriveOutEnable | read |
| MainMachineStop | bool | MainMachineStop | read |
| TriggerMaintenance | bool | TriggerMaintenance | read |
| IsAlarmActive | bool | (always false this round) | — |
| CalibrationMessage | string | write/validation feedback | — |
| KeypadTarget | enum None/In/Out | UI state | — |
| KeypadEditValue | string | keypad buffer | — |
| HomingRefPosInInvalid | bool | local validation | — |
| HomingRefPosOutInvalid | bool | local validation | — |

Snapshot: existing `updateTimer` 500ms Tick copies `CurrentValue[VarName]` via `SetProperty`. Skip overwriting HomingRefPos*Text while `KeypadTarget` matches that axis. InvariantCulture. Missing key keeps last value (initial "0.00"/false).

Writes: lookup VarAddress by VarName across all GroupList.VarList, then `await CommonMethods.plc.WriteNodeAsync<T>(address, value)`. Bool for commands, float for origin. Failures set CalibrationMessage. Pump never writes.

---

## Verification Strategy (MANDATORY)

### Test Decision
- **Infrastructure exists**: NO dedicated test project
- **Automated tests**: None
- **Framework**: none
- **Agent-Executed QA**: ALWAYS

### QA Policy
Every task includes agent-executed QA. Evidence under `.omo/evidence/`.
- Library: Bash / PowerShell inspect files
- Build: `dotnet build CX102PrickHMI.sln -c Debug`
- UI: cannot press live PLC from this agent; verify XAML bindings, styles, and compile. Hardware round-trip is a user check listed in Final Wave.

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Start Immediately):
├── Task 1: OpenSpec artifacts [writing]
├── Task 2: MomentaryButtonBehavior [quick]
├── Task 3: NumericKeypadOverlay [visual-engineering]
├── Task 4: Hold button + input styles [visual-engineering]
└── Task 5: PlcTagLookup helper [quick]

Wave 2 (After Wave 1):
└── Task 6: MainViewModel notify props, pump, writes, uncomment poll, strip mocks [deep]

Wave 3 (After Wave 2):
├── Task 7: MonitorView.xaml real bindings [visual-engineering]
└── Task 8: Alarm/History empty lists + DateSummary + MainWindow empty Steps [quick]

Wave FINAL:
├── Task F1: Plan compliance audit (oracle)
├── Task F2: Code quality review (unspecified-high)
├── Task F3: Real QA (unspecified-high)
└── Task F4: Scope fidelity check (deep)

Critical Path: 1+5 → 6 → 7 → F1-F4
Max Concurrent: 5 (Wave 1)
```

### Dependency Matrix

- **1-5**: none — 6, 7
- **6**: 1, 2, 5 — 7, 8
- **7**: 3, 4, 6 — F
- **8**: 6 — F

### Agent Dispatch Summary

- **Wave 1**: 5 — T1 writing, T2 quick, T3 visual-engineering, T4 visual-engineering, T5 quick
- **Wave 2**: 1 — T6 deep
- **Wave 3**: 2 — T7 visual-engineering, T8 quick
- **FINAL**: 4 — F1 oracle, F2 unspecified-high, F3 unspecified-high, F4 deep

---

## TODOs

- [x] 1. Write OpenSpec artifacts for bind-monitor-plc-points

  **What to do**:
  - CLI: `npx --yes @fission-ai/openspec` from `F:\CX102PrickHMI`. Change `bind-monitor-plc-points` may already be scaffolded; if missing run `new change bind-monitor-plc-points`.
  - Write `proposal.md`, `specs/monitor-plc-binding/spec.md`, `design.md`, `tasks.md` from `openspec instructions <id> --change bind-monitor-plc-points --json` templates.
  - Spec MUST include: notify-property contract, momentary press/release, keypad >0 F2, MainMachineStop polarity, uncomment-only poll, empty alarm/history, commented SQLite Test.
  - Run `npx --yes @fission-ai/openspec status --change bind-monitor-plc-points` until proposal/specs/design/tasks are done.

  **Must NOT do**:
  - Do not implement C#/XAML in this task
  - Do not `openspec init`

  **Recommended Agent Profile**:
  - **Category**: `writing`
    - Reason: OpenSpec planning artifacts only
  - **Skills**: [`openspec-propose`]
  - **Skills Evaluated but Omitted**:
    - `openspec-apply-change`: implementation later

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: 6
  - **Blocked By**: None

  **References**:
  - `openspec/config.yaml` — spec-driven schema
  - `.omo/plans/bind-monitor-plc-points.md` — locked decisions
  - `.omo/drafts/bind-monitor-plc-points.md` — interview record
  - Grill decisions: MainMachineStop false=green; origin >0; keypad overlay; WriteNodeAsync

  **Acceptance Criteria**:
  - [ ] Files exist: `openspec/changes/bind-monitor-plc-points/proposal.md`, `design.md`, `tasks.md`, `specs/monitor-plc-binding/spec.md`
  - [ ] `npx --yes @fission-ai/openspec status --change bind-monitor-plc-points` shows those artifacts done

  **QA Scenarios**:

  ```
  Scenario: OpenSpec artifacts present
    Tool: Bash
    Preconditions: cwd F:\CX102PrickHMI
    Steps:
      1. Test-Path each of the four artifact files
      2. npx --yes @fission-ai/openspec status --change bind-monitor-plc-points
    Expected Result: all four True; status not missing those artifacts
    Failure Indicators: missing file or status blocked on proposal
    Evidence: .omo/evidence/task-1-openspec-status.txt
  ```

  **Evidence to Capture**:
  - [ ] task-1-openspec-status.txt

  **Commit**: NO (groups with 8)

- [x] 2. Add MomentaryButtonBehavior

  **What to do**:
  - Create `CX102PrickHMI/Behaviors/MomentaryButtonBehavior.cs` as attached behavior on Button (DependencyProperty, no extra NuGet).
  - On PreviewMouseLeftButtonDown + CaptureMouse: invoke bound ICommand with `true` (or bool parameter).
  - On PreviewMouseLeftButtonUp + LostMouseCapture: invoke with `false`.
  - Cover touch via WPF mouse promotion. No PLC writes in the behavior — only command invoke.
  - Add file to csproj Compile Include if the project is non-SDK (explicit Compile list).

  **Must NOT do**:
  - Do not add Microsoft.Xaml.Behaviors package
  - Do not write OPC in the behavior

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: one small class
  - **Skills**: [`wpf-dev-pack:make-wpf-behavior`]
    - Reason: WPF attached behavior pattern
  - **Skills Evaluated but Omitted**:
    - `implementing-communitytoolkit-mvvm`: VM later

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: 6, 7
  - **Blocked By**: None

  **References**:
  - `styles/Controls.xaml:17-37` — NavButtonStyle template (no IsPressed visual yet)
  - MainWindow.xaml.cs — empty view pattern to preserve

  **Acceptance Criteria**:
  - [ ] File exists and compiles
  - [ ] Behavior has press→true and release/lost-capture→false command invoke
  - [ ] csproj includes the file if needed

  **QA Scenarios**:

  ```
  Scenario: Behavior source has press and release paths
    Tool: Bash / Grep
    Preconditions: file written
    Steps:
      1. Grep MomentaryButtonBehavior.cs for PreviewMouseLeftButtonDown, PreviewMouseLeftButtonUp, LostMouseCapture
    Expected Result: all three present
    Failure Indicators: missing LostMouseCapture
    Evidence: .omo/evidence/task-2-behavior-grep.txt
  ```

  **Evidence to Capture**:
  - [ ] task-2-behavior-grep.txt

  **Commit**: NO

- [x] 3. Add NumericKeypadOverlay UserControl

  **What to do**:
  - Create `Views/NumericKeypadOverlay.xaml` + `.xaml.cs` (InitializeComponent only).
  - Dark overlay matching PageBackground/Surface/Primary. Keys: 0-9, decimal, backspace, 确认, 取消.
  - Bind to MainViewModel: KeypadEditValue, ConfirmKeypadCommand, CancelKeypadCommand, KeypadTarget (Visibility when not None).
  - No system TextBox focus on overlay keys (Buttons only). Two decimal display convention.
  - Add to csproj Page/Compile if explicit.

  **Must NOT do**:
  - Do not use a Window
  - Do not put write logic in code-behind

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: themed overlay UI
  - **Skills**: [`wpf-dev-pack:make-wpf-usercontrol`, `frontend-ui-ux`]
  - **Skills Evaluated but Omitted**:
    - `make-wpf-custom-control`: overlay is a UserControl not a templated control

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: 7
  - **Blocked By**: None

  **References**:
  - `styles/Colors.xaml` — PageBackground #0F1115, Surface #1A1D23, Primary #0EA5E9, Error #EF4444
  - `styles/Controls.xaml:107-126` — PrimaryButtonStyle / InputStyle

  **Acceptance Criteria**:
  - [ ] Overlay XAML exists with digit/decimal/backspace/confirm/cancel
  - [ ] Visibility bound to KeypadTarget
  - [ ] code-behind has no business logic
  - [ ] `dotnet build` after later waves still green (this wave: file compiles when included)

  **QA Scenarios**:

  ```
  Scenario: Overlay keys exist
    Tool: Grep
    Preconditions: XAML written
    Steps:
      1. Grep NumericKeypadOverlay.xaml for 确认, 取消, Content="0" through Content="9"
    Expected Result: confirm, cancel, digits present
    Failure Indicators: missing 确认 or decimal key
    Evidence: .omo/evidence/task-3-keypad-keys.txt
  ```

  **Evidence to Capture**:
  - [ ] task-3-keypad-keys.txt

  **Commit**: NO

- [x] 4. Add hold-button and readonly-input styles

  **What to do**:
  - In `styles/Controls.xaml` add `HoldButtonStyle` (TargetType Button): released Background Surface2 (#232730), Foreground Ink, Border Border; IsPressed Background Primary (#0EA5E9), Foreground White. Distinct from NavButtonStyle / MonitorNavButtonStyle.
  - Add `ReadonlyValueBoxStyle` based on InputStyle: IsReadOnly true, Focusable false (blocks TabTip). Optional red Border when HomingRefPosInInvalid / OutInvalid via DataTrigger (or apply trigger on the boxes in MonitorView).
  - Merge still goes through Generic.xaml — only edit Controls.xaml.

  **Must NOT do**:
  - Do not change NavButtonStyle pressed look
  - Do not add a new ResourceDictionary file unless Generic already merges it

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: theme-consistent styles
  - **Skills**: [`wpf-dev-pack:managing-styles-resourcedictionary`]
  - **Skills Evaluated but Omitted**:
    - `frontend-design`: desktop WPF not web

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: 7
  - **Blocked By**: None

  **References**:
  - `styles/Colors.xaml:5,9,12` — Surface2, Primary, Ink
  - `styles/Controls.xaml:17-118` — Nav vs Primary vs Secondary
  - `styles/Generic.xaml` — merge order

  **Acceptance Criteria**:
  - [ ] HoldButtonStyle exists with IsPressed → Primary
  - [ ] ReadonlyValueBoxStyle IsReadOnly+Focusable false

  **QA Scenarios**:

  ```
  Scenario: Hold style keys exist
    Tool: Grep
    Preconditions: Controls.xaml edited
    Steps:
      1. Grep styles/Controls.xaml for x:Key="HoldButtonStyle" and x:Key="ReadonlyValueBoxStyle"
      2. Grep HoldButtonStyle block for IsPressed and Surface2 or #232730
    Expected Result: both keys present; pressed uses Primary
    Failure Indicators: HoldButtonStyle BasedOn NavButtonStyle with DataTrigger IsMonitorSelected
    Evidence: .omo/evidence/task-4-styles.txt
  ```

  **Evidence to Capture**:
  - [ ] task-4-styles.txt

  **Commit**: NO

- [x] 5. Add PlcTagLookup helper

  **What to do**:
  - Create `Utilities/PlcTagLookup.cs` (or `CX102PrickHMI/Utilities/` matching CLAUDE.md).
  - `TryGetAddress(OPCUADevice device, string varName, out string address)` iterates all GroupList.VarList, ordinal ignore-case match on VarName, returns VarAddress.
  - No static cache required. Null-safe. Add to csproj Compile if needed.

  **Must NOT do**:
  - Do not hardcode group name "Alarm"
  - Do not write PLC here

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: one helper
  - **Skills**: []
  - **Skills Evaluated but Omitted**:
    - `api-and-interface-design`: internal helper not a public API

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: 6
  - **Blocked By**: None

  **References**:
  - `ViewModels/MainViewModel.cs:452-472` — VarList VarName/VarAddress/CurrentValue pattern
  - `bin/Debug/Settings/settings.json` — tag names

  **Acceptance Criteria**:
  - [ ] Helper compiles
  - [ ] Iterates all groups, not one named group

  **QA Scenarios**:

  ```
  Scenario: Lookup iterates GroupList
    Tool: Grep
    Preconditions: file written
    Steps:
      1. Grep PlcTagLookup.cs for GroupList and VarAddress and VarName
    Expected Result: all three present; no hardcoded "Alarm"
    Failure Indicators: string literal Alarm as group filter
    Evidence: .omo/evidence/task-5-lookup-grep.txt
  ```

  **Evidence to Capture**:
  - [ ] task-5-lookup-grep.txt

  **Commit**: NO

- [x] 6. MainViewModel: notify properties, snapshot pump, async writes, uncomment poll, strip mocks

  **What to do**:
  - Uncomment `GetOPCUAValue` and the `Task.Run` in `AttemptConnectionAndInitialData` with ZERO edits inside the method body.
  - Implement the notify-property contract from this plan (SetProperty, InvariantCulture F2).
  - `UpdateTimer_Tick`: copy CurrentValue → properties; skip HomingRefPos*Text while KeypadTarget is that axis.
  - Commands: HoldHomingIn/Out/Setup (bool param via momentary behavior); OpenKeypadIn/Out; ConfirmKeypad (parse > 0, WriteNodeAsync float); CancelKeypad.
  - WriteNodeAsync bool/float after PlcTagLookup. Failures → CalibrationMessage. Initial CalibrationMessage empty string (not 模拟数据).
  - Comment SQLite Test insert (keep code, commented).
  - Delete _simulationTimer / AdvanceSimulation / InitializeSteps seed content: keep Steps as empty ObservableCollection so MainWindow ItemsControl shows empty lamps.
  - InitializeAlarmSeeds: empty AlarmItems and HistoryItems; DateSummary empty or " ".
  - IsAlarmActive always false.
  - Remove CalibrateCommand/SettingsCommand/MaintenanceCommand mock commands.
  - Manual SetProperty only. CommunityToolkit ObservableObject.

  **Must NOT do**:
  - Do not edit GetOPCUAValue internals
  - Do not use [ObservableProperty]
  - Do not write from the pump
  - Do not edit dead per-page ViewModels
  - Do not change heartbeat / PLCCOM timing

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: core VM + OPC glue
  - **Skills**: [`wpf-dev-pack:implementing-communitytoolkit-mvvm`, `dotnet-wpf`]
  - **Skills Evaluated but Omitted**:
    - `tdd`: no test project

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 2
  - **Blocks**: 7, 8
  - **Blocked By**: 1, 2, 5

  **References**:
  - `ViewModels/MainViewModel.cs:36-114` ctor, empty Tick
  - `ViewModels/MainViewModel.cs:147-181` mock properties
  - `ViewModels/MainViewModel.cs:415-490` commented poll
  - `CommonMethods.cs:16-18` plcDevice / plc
  - HandMirror: WriteNodeAsync<T> on thinger.CommunicationLib.OPCUA

  **Acceptance Criteria**:
  - [ ] GetOPCUAValue method is uncommented
  - [ ] All contract properties exist with SetProperty
  - [ ] UpdateTimer_Tick reads CurrentValue
  - [ ] WriteNodeAsync used; pump has no Write
  - [ ] SQLite insert is comments
  - [ ] `dotnet build CX102PrickHMI.sln -c Debug` PASS

  **QA Scenarios**:

  ```
  Scenario: Build and poll uncommented
    Tool: Bash
    Preconditions: VM edited
    Steps:
      1. Grep MainViewModel.cs for "private async Task GetOPCUAValue" not preceded by //
      2. Grep for WriteNodeAsync
      3. Grep for 模拟数据 — must not remain as active string
      4. dotnet build CX102PrickHMI.sln -c Debug --nologo
    Expected Result: method live; WriteNodeAsync present; build 0 errors
    Failure Indicators: GetOPCUAValue still fully commented; build fail
    Evidence: .omo/evidence/task-6-build.txt

  Scenario: Pump does not write
    Tool: Grep
    Preconditions: UpdateTimer_Tick implemented
    Steps:
      1. Read UpdateTimer_Tick body
      2. Assert no WriteNode / WriteNodeAsync inside Tick
    Expected Result: read-only snapshot
    Failure Indicators: write in Tick
    Evidence: .omo/evidence/task-6-pump-no-write.txt
  ```

  **Evidence to Capture**:
  - [ ] task-6-build.txt
  - [ ] task-6-pump-no-write.txt

  **Commit**: NO

- [x] 7. Bind MonitorView.xaml to MainViewModel PLC properties

  **What to do**:
  - Recipe/actual TextBlocks: RecipeRing1/2, ActualRing1/2. Fix line 114 label 内侧刺针环 → 外侧刺针环 for the second actual value.
  - Replace 校准 / 设置 / 维修模式 buttons: three HoldButtonStyle buttons 校准内侧, 校准外侧, 设置 in one Horizontal StackPanel, MomentaryButtonBehavior + HoldHomingIn/Out/Setup commands.
  - Remove Maintenance button. Add TextBlock 维修模式 激活/未激活 bound to TriggerMaintenance (green Success vs Ink3) via DataTrigger — text AND color.
  - Homing lamps: StsHomingDoneIn/Out DataTrigger Fill Success vs Ink3; text 已回原 / 未回原.
  - 主机: bind MainMachineStop — false Fill Success text 主机运行中; true Fill Ink3 text 主机已停机. Change current hardcoded 系统运行中 if that is the host-run row (MonitorView.xaml ~300).
  - Enable rows: DriveInEnable / DriveOutEnable lamps+text 已使能 / 未使能.
  - Origin boxes: ReadonlyValueBoxStyle bound HomingRefPosInText/OutText; click opens keypad (command OpenKeypadIn/Out). Invalid red border via HomingRefPosInInvalid.
  - Host NumericKeypadOverlay on MonitorView Grid overlay (full cell).
  - CalibrationMessage keep, bind existing TextBlock.
  - d:DataContext may stay MonitorViewModel (design-time only) OR switch to MainViewModel — do not edit MonitorViewModel.cs.
  - Title of origin inputs: 内侧伺服 校准原点值 / 外侧伺服 校准原点值 (not 测量当前实际值).

  **Must NOT do**:
  - Do not bind process steps
  - Do not re-enable IsAlarmActive pulse
  - Do not put write logic in MonitorView.xaml.cs
  - Do not change 报警 0 条 row

  **Recommended Agent Profile**:
  - **Category**: `visual-engineering`
    - Reason: Monitor layout and triggers
  - **Skills**: [`dotnet-wpf`, `wpf-dev-pack:advanced-data-binding`]
  - **Skills Evaluated but Omitted**:
    - `frontend-design`: WPF not web

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3 (with 8)
  - **Blocks**: Final
  - **Blocked By**: 3, 4, 6

  **References**:
  - `Views/MonitorView.xaml:38-175` recipe/actual/buttons
  - `Views/MonitorView.xaml:191-219` homing lamps
  - `Views/MonitorView.xaml:295-344` device status rows
  - `styles/Colors.xaml` Success / Ink3 / Error

  **Acceptance Criteria**:
  - [ ] All contract properties referenced in MonitorView.xaml
  - [ ] Outer actual label is 外侧
  - [ ] Three hold buttons, no 维修模式 Button
  - [ ] MonitorView.xaml.cs still only InitializeComponent
  - [ ] Build PASS

  **QA Scenarios**:

  ```
  Scenario: Bindings present
    Tool: Grep
    Preconditions: MonitorView.xaml updated
    Steps:
      1. Grep MonitorView.xaml for RcpActIn OR RecipeRing1, ActPosIn OR ActualRing1, StsHomingDoneIn, DriveInEnable, MainMachineStop, TriggerMaintenance, HomingRefPosInText, CmdHomingIn OR HoldHomingIn
      2. Grep for 外侧刺针环 on the actual-value column
      3. Grep for 维修模式 Button — must be 0 Button with that Content
    Expected Result: PLC bindings present; outer label fixed; no maintenance Button
    Failure Indicators: still Binding Servo1Actual; Content="维修模式" on Button
    Evidence: .omo/evidence/task-7-monitor-bindings.txt
  ```

  **Evidence to Capture**:
  - [ ] task-7-monitor-bindings.txt

  **Commit**: NO

- [x] 8. Empty alarm/history seeds and unbind process-step mock animation

  **What to do**:
  - AlarmItems/HistoryItems remain as properties, constructed empty (no seed AlarmEntry).
  - DateSummary: empty string; HistoryAlarmView.xaml still binds it (shows blank).
  - Steps: empty collection; MainWindow.xaml ItemsControl stays bound to Steps (shows empty row of lamps). Do not bind PLC step tags.
  - Confirm simulation timer removed in Task 6; if leftover, remove here.
  - Do not change AlarmListView/HistoryAlarmView item templates.

  **Must NOT do**:
  - Do not delete AlarmItems/HistoryItems/Steps properties
  - Do not bind StsStep* tags
  - Do not edit AlarmListViewModel.cs / HistoryAlarmViewModel.cs

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: seed cleanup
  - **Skills**: []
  - **Skills Evaluated but Omitted**:
    - `virtualizing-wpf-ui`: lists stay small/empty

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3 (with 7)
  - **Blocks**: Final
  - **Blocked By**: 6

  **References**:
  - `ViewModels/MainViewModel.cs:239-268` seeds
  - `MainWindow.xaml:67` Steps ItemsControl
  - `Views/HistoryAlarmView.xaml:87` DateSummary

  **Acceptance Criteria**:
  - [ ] No new AlarmEntry("2026-07-29 in InitializeAlarmSeeds
  - [ ] Steps initialized empty
  - [ ] Build PASS

  **QA Scenarios**:

  ```
  Scenario: Mock seeds gone
    Tool: Grep
    Preconditions: Task 6+8 done
    Steps:
      1. Grep MainViewModel.cs for 伺服1 回原超时 and 125.00
      2. Grep MainViewModel.cs for _simulationTimer
    Expected Result: no seed strings; no simulation timer field
    Failure Indicators: seed dates still constructed
    Evidence: .omo/evidence/task-8-no-seeds.txt
  ```

  **Evidence to Capture**:
  - [ ] task-8-no-seeds.txt

  **Commit**: YES
  - Message: `feat(hmi): bind monitor page to PLC tags`
  - Files: OpenSpec change, MainViewModel, MonitorView, keypad, behavior, styles, lookup, csproj
  - Pre-commit: `dotnet build CX102PrickHMI.sln -c Debug`

---

## Final Verification Wave

> 4 review agents run in PARALLEL. ALL must APPROVE. Present consolidated results to user and get explicit "okay" before completing.

- [x] F1. **Plan Compliance Audit** — `oracle` — VERDICT: APPROVE (Must Have 6/6 | Must NOT Have 9/9 | Tasks 8/8)
  Read the plan end-to-end. For each Must Have verify implementation exists. For each Must NOT Have search codebase. Check evidence in `.omo/evidence/`.
  Output: `Must Have [N/N] | Must NOT Have [N/N] | Tasks [N/N] | VERDICT: APPROVE/REJECT`

- [x] F2. **Code Quality Review** — `unspecified-high` — VERDICT: APPROVE (Build PASS | 3 clean/3 minor, no blockers)
  `dotnet build CX102PrickHMI.sln -c Debug`. Review changed files for `as any`/`@ts-ignore` N/A; C#: empty catches, console.log, commented-out blocks except the required SQLite Test comment. No `[ObservableProperty]`. No Brushes in VM.
  Output: `Build [PASS/FAIL] | Files [N clean/N issues] | VERDICT`

- [x] F3. **Real Manual QA** — `unspecified-high` — VERDICT: APPROVE (Scenarios 9/9 pass; evidence f3-qa.txt)
  Verify XAML bindings listed in Task 7 exist. grep Monitor path for 125.00, 198.50, 模拟数据. Confirm GetOPCUAValue is uncommented. Hardware PLC round-trip is user-side after handoff.
  Output: `Scenarios [N/N pass] | VERDICT`

- [x] F4. **Scope Fidelity Check** — `deep` — VERDICT: APPROVE (Tasks 8/8 compliant | Contamination CLEAN)
  Diff vs plan. No OPC loop timing changes. No TIA. No process-step PLC bind. No dead-VM edits. No new NuGet.
  Output: `Tasks [N/N compliant] | Contamination [CLEAN/N issues] | VERDICT`

---

## Commit Strategy

- **1**: `feat(hmi): bind monitor page to PLC tags` — MainViewModel, MonitorView, keypad, behavior, styles, OpenSpec

---

## Success Criteria

### Verification Commands
```bash
dotnet build CX102PrickHMI.sln -c Debug --nologo
npx --yes @fission-ai/openspec status --change bind-monitor-plc-points
```

### Final Checklist
- [x] All Must Have present (F1: 6/6)
- [x] All Must NOT Have absent (F1: 9/9, F4: Contamination CLEAN)
- [x] Build 0 errors (0 warnings)
- [x] Notify properties listed in the contract all exist on MainViewModel (F3 S1: 31/31 bindings resolve)

### Commit Outcome
- [~] Commit SKIPPED — F:\CX102PrickHMI is not a git repository (`git rev-parse` fatal). Not initializing git without user instruction. Working tree changes remain on disk; user may `git init` + commit later.
