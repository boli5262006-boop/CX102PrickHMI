# Proposal

## Why

The Monitor page currently displays hardcoded simulation data (125.00 / 198.50, mock alarm and history seeds, a simulation timer) instead of live PLC values, and its buttons write mock strings instead of PLC points. The HMI must operate the real machine, so the Monitor page needs to read actual PLC tags from `settings.json` points and write momentary commands and validated origin values back to the PLC.

## What Changes

- Bind the Monitor page to real PLC tags via `MainViewModel` notify properties fed by a 500 ms snapshot pump copying `CurrentValue[VarName]`:
  - Recipe/actual values: `RcpActIn`/`RcpActOut`, `ActPosIn`/`ActPosOut` (readonly text, 2 decimals).
  - Status lamps + text: `StsHomingDoneIn`/`StsHomingDoneOut`, `DriveInEnable`/`DriveOutEnable`, `MainMachineStop` (false = green 主机运行中, true = gray 主机已停机), `TriggerMaintenance` (维修模式 激活/未激活 text block replacing the maintenance button).
- Momentary PLC command buttons (hold-to-press): `CmdHomingIn` / `CmdHomingOut` / `CmdSetup` — press writes `true`, release or lost mouse capture writes `false`, via a new attached `MomentaryButtonBehavior` and hold-button styles.
- Origin value editing: readonly `HomingRefPosIn` / `HomingRefPosOut` boxes opened with an on-screen numeric keypad overlay; the write happens only on 确认 confirm, value must be > 0, formatted with 2 decimals in `InvariantCulture`; write resolves `VarAddress` by `VarName` lookup then `OPCUA.WriteNodeAsync<float>`; command buttons write via `WriteNodeAsync<bool>`.
- Uncomment the existing OPC read pump (`GetOPCUAValue` and its `Task.Run` caller) with zero internal edits — without it `CurrentValue` stays empty.
- Remove mock data: simulation timer and its advance logic, seeded alarm/history lists and fake `DateSummary`, hardcoded recipe/actual strings; keep the SQLite Test insert commented (not deleted).
- Unbound on purpose this round: top 9 process-step lamps (stay empty), device-image alarm pulse (`IsAlarmActive` always false), OPC poll timing, startup async, TIA Portal, Overlap page, header SystemStatus / footer FooterStatus, 「报警 0 条」 row.

## Capabilities

### New Capabilities

- `monitor-plc-binding`: Binding the Monitor page to live PLC points — read snapshot pump onto notify properties, momentary command writes, validated origin keypad writes, and removal of all simulation/mock data from the Monitor path.

### Modified Capabilities

<!-- None. This is the first capability spec; openspec/specs/ is empty. -->

## Impact

- **Affected code**: `ViewModels/MainViewModel.cs` (notify properties, 500 ms snapshot pump, async write commands, uncomment poll, mock removal), `Views/MonitorView.xaml` (real bindings, DataTrigger lamps, hold buttons, keypad overlay host), `styles/Controls.xaml` (HoldButtonStyle, ReadonlyValueBoxStyle), new `Behaviors/MomentaryButtonBehavior.cs`, new `Views/NumericKeypadOverlay.xaml`/`.cs`, new `Utilities/PlcTagLookup.cs`, csproj includes.
- **Interfaces**: `thinger.CommunicationLib.OPCUA.WriteNodeAsync<T>` (existing, bool/float overloads); `CurrentValue[VarName]` dictionary populated by `GetOPCUAValue`; tag names/addresses from `Settings/settings.json` (`GroupList.VarList`).
- **Out of scope**: OPC poll delay/heartbeat/startup async changes, dead per-page ViewModels (`MonitorViewModel` etc.), AlarmList/HistoryAlarm views' item templates, TIA Portal project, process-step PLC tags, new NuGet packages.
