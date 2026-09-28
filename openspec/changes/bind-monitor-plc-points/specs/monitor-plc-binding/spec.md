# Spec Delta

## Purpose

Lets the Monitor page of the prick HMI display and control real PLC points: live tag values from the existing OPC read pump, hold-to-press machine commands, and validated calibration-origin writes from an on-screen numeric keypad, while all simulation/mock data is removed from the Monitor path.

## ADDED Requirements

### Requirement: Monitor page displays live PLC values

The Monitor page SHALL display recipe and actual ring values (`RcpActIn`, `RcpActOut`, `ActPosIn`, `ActPosOut`) and status states (`StsHomingDoneIn`, `StsHomingDoneOut`, `DriveInEnable`, `DriveOutEnable`, `MainMachineStop`, `TriggerMaintenance`) sourced from the PLC `CurrentValue[VarName]` snapshot copied onto MainViewModel notify properties by the existing 500 ms update timer. Numeric text SHALL be formatted with exactly 2 decimal places using the invariant culture. When a tag key is missing from the snapshot, the view SHALL keep the last displayed value (initial "0.00" / false).

#### Scenario: Recipe and actual values come from the PLC snapshot
- **WHEN** the PLC reports `RcpActIn` = 125.00 and `ActPosOut` = 198.50 and the update timer ticks
- **THEN** the Monitor recipe/actual fields show "125.00" and "198.50" with no hardcoded values remaining in the Monitor path

#### Scenario: Main machine stop polarity
- **WHEN** `MainMachineStop` is false
- **THEN** the host status lamp is green with text 主机运行中
- **WHEN** `MainMachineStop` is true
- **THEN** the host status lamp is gray with text 主机已停机

#### Scenario: Maintenance state is display-only
- **WHEN** `TriggerMaintenance` is true
- **THEN** the Monitor page shows 维修模式 激活 (active styling) instead of a maintenance command button
- **WHEN** `TriggerMaintenance` is false
- **THEN** the Monitor page shows 维修模式 未激活

### Requirement: Momentary command buttons write press and release to the PLC

The Monitor page SHALL provide hold-to-press buttons for `CmdHomingIn`, `CmdHomingOut`, and `CmdSetup` such that pressing the button writes `true` to the PLC point and releasing it (or losing mouse capture) writes `false`. The button SHALL hold its pressed visual state while held. No separate confirmation or toggle behavior is allowed for these commands.

#### Scenario: Hold-to-write cycle
- **WHEN** the user presses and holds a homing command button
- **THEN** `true` is written to the corresponding PLC point and the button shows its pressed style
- **WHEN** the user releases the button (or the mouse capture is lost)
- **THEN** `false` is written to the same PLC point and the button returns to its released style

#### Scenario: Three separate momentary commands
- **WHEN** the Monitor page renders the command row
- **THEN** there are exactly three momentary buttons (校准内侧, 校准外侧, 设置) bound to `CmdHomingIn`, `CmdHomingOut`, `CmdSetup` and no 维修模式 command button

### Requirement: Origin values are edited through a validated on-screen keypad

The calibration origin values (`HomingRefPosIn`, `HomingRefPosOut`) SHALL be shown in readonly boxes. Activating a box SHALL open the on-screen numeric keypad overlay for that axis. The PLC write SHALL occur only when the user confirms (确认) with a value greater than 0; the value SHALL be written as a float with 2-decimal invariant-culture formatting. Input failing validation SHALL be rejected with visible feedback and SHALL NOT be written. Cancel (取消) SHALL discard the edit and restore the displayed value.

#### Scenario: Confirm writes a valid origin value
- **WHEN** the user opens the keypad for the inner axis, enters 120.5, and presses 确认
- **THEN** the value 120.50 is written to the `HomingRefPosIn` PLC point and the box shows "120.50"

#### Scenario: Invalid value is rejected
- **WHEN** the user enters 0 or a non-positive value and presses 确认
- **THEN** the edit is rejected, a validation message is shown, the box is flagged invalid, and no PLC write occurs

#### Scenario: Cancel discards the edit
- **WHEN** the user presses 取消 while the keypad is open
- **THEN** the overlay closes and the origin box keeps its previous displayed value with no PLC write

#### Scenario: Pump does not clobber an open keypad edit
- **WHEN** the keypad overlay is open for an axis and the 500 ms snapshot tick occurs
- **THEN** the origin text of the axis being edited is not overwritten by the snapshot

### Requirement: PLC writes resolve tag addresses by name and report failures

All Monitor-page writes SHALL resolve the target `VarAddress` by looking up the `VarName` across all configured tag groups, then SHALL issue an asynchronous OPC write (`WriteNodeAsync<bool>` for commands, `WriteNodeAsync<float>` for origin values). Write or lookup failures SHALL be reported in the on-page calibration feedback message and SHALL NOT crash the application. The 500 ms snapshot pump SHALL perform reads only and MUST NOT write to the PLC.

#### Scenario: Command write uses name-resolved address
- **WHEN** a momentary command writes `CmdHomingIn`
- **THEN** the address is resolved from the tag configuration by `VarName` lookup and written asynchronously as a bool

#### Scenario: Write failure feedback
- **WHEN** a PLC write fails (e.g., tag name not found or communication error)
- **THEN** the calibration feedback message on the Monitor page describes the failure and the application keeps running

### Requirement: OPC read pump is enabled

The existing OPC read loop (`GetOPCUAValue` and its startup task) SHALL be enabled by uncommenting it with zero internal edits, so that `CurrentValue[VarName]` is populated from the PLC. Poll timing, heartbeat, and startup sequencing SHALL remain unchanged.

#### Scenario: CurrentValue is populated
- **WHEN** the application starts and the PLC connection is established
- **THEN** the read loop runs and `CurrentValue[VarName]` receives values for the configured tags, so the Monitor page stops showing initial defaults

### Requirement: Simulation and mock data are removed from the Monitor path

The Monitor path SHALL contain no simulation: the simulation timer and its advance logic SHALL be deleted, alarm and history lists SHALL be kept as (empty) properties with all seed entries removed, `DateSummary` SHALL be blank, and hardcoded recipe/actual strings SHALL be removed. The SQLite test insert SHALL be commented out but retained in source for later use.

#### Scenario: No mock seeds remain
- **WHEN** the change is complete
- **THEN** the Monitor path contains no hardcoded 125.00 / 198.50 / 模拟数据 strings, the simulation timer no longer exists, and alarm/history lists initialize empty

#### Scenario: SQLite test insert is preserved but disabled
- **WHEN** the change is complete
- **THEN** the SQLite Test insert code is present as comments and produces no database rows

### Requirement: Explicit non-binding boundaries for this change

The top 9 process-step lamps SHALL remain unbound (empty collection displayed) and the device-image alarm pulse SHALL stay disabled (`IsAlarmActive` always false). OPC poll timing, startup async structure, the Overlap page, header SystemStatus / footer FooterStatus, and the 「报警 0 条」 row SHALL NOT be modified by this capability.

#### Scenario: Process-step lamps and alarm pulse stay unbound
- **WHEN** the change is complete
- **THEN** no PLC step tags are bound to the process-step lamps, the lamps render from an empty collection, and `IsAlarmActive` is always false

#### Scenario: Out-of-scope surfaces untouched
- **WHEN** the change is implemented
- **THEN** OPC poll delay, startup async flow, TIA Portal assets, the Overlap page, header SystemStatus, footer FooterStatus, and the 报警 0 条 row behave exactly as before
