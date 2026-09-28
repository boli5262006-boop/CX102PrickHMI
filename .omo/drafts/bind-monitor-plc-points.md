# Draft: bind-monitor-plc-points

## Requirements (confirmed)
- Remove all simulation data (monitor sim timer, alarm/history fake lists); keep SQLite Test insert commented not deleted
- Bind Monitor page to PLC via CurrentValue[VarName] with INotify
- Momentary buttons: CmdHomingIn, CmdHomingOut, CmdSetup — press true, release/lost-capture false
- Split calibrate into 校准内侧 / 校准外侧; keep 设置; replace 维修按钮 with TriggerMaintenance status TextBlock
- Status lamps + text follow bool: StsHomingDoneIn/Out, DriveInEnable/OutEnable, MainMachineStop (false=green running, true=gray stopped)
- HomingRefPosIn/Out: readonly box + center numeric keypad overlay; write on confirm only; must be > 0
- Recipe/actual: RcpActIn/Out, ActPosIn/Out readonly
- Scope B: do not bind top 9 process steps this round; disable device-image alarm pulse (IsAlarmActive false)
- Do not change OPC poll loop throttling / startup async
- Write via VarName lookup of VarAddress then WriteNode<T>(address, value)
- OpenSpec artifacts then implement

## Technical Decisions
- All page DataContext = MainViewModel (already done)
- 500ms updateTimer already exists empty — use for dictionary snapshot
- Keypad: overlay A, 2 decimal places, box readonly
- Press style: Primary #0EA5E9 pressed, Surface2 #232730 released
- CalibrationMessage kept for real feedback only

## Research Findings
- OPCUA.WriteNode<T>(string, T) exists on thinger.CommunicationLib
- Read already uses NodeId(VarAddress) and stores CurrentValue[VarName]
- settings.json Alarm group has all needed tags
- MonitorView.xaml line 114 mislabels outer actual as 内侧 — fix in this change

## Open Questions
- None remaining after grill rounds

## Defaults Applied (Metis follow-up)
- Uncomment GetOPCUAValue + Task.Run as-is (zero edits inside). Without this CurrentValue stays empty.
- Writes use WriteNodeAsync; failures go to CalibrationMessage; no MessageBox
- Momentary via attached behavior (xaml.cs stays empty)
- Single keypad overlay + KeypadTarget (None/In/Out); prefill F2; no upper bound
- Alarm/history: keep properties, empty collections, remove fake DateSummary text
- Header SystemStatus / FooterStatus / 报警 0 条: out of scope
- Dead per-page ViewModels: do not edit
- MVVM: all bound PLC values are MainViewModel SetProperty properties; XAML DataTriggers for lamp color+text; no Brushes in VM; no [ObservableProperty]

## Scope Boundaries
- INCLUDE: Monitor PLC bind, keypad, momentary buttons, remove mock data, comment Test insert, OpenSpec change bind-monitor-plc-points
- EXCLUDE: process-step lamps, alarm pulse from fault tags, OPC loop delay, startup defer, TIA Portal, Overlap page

## Test Strategy Decision
- Infrastructure exists: NO dedicated test project in solution
- Automated tests: NO
- Agent-Executed QA: ALWAYS
