# Spec Delta

## Purpose

This capability gives Monitor-page operators dedicated forward and backward manual jog controls while retaining the existing hold-to-press PLC command semantics used by calibration controls.

## ADDED Requirements

### Requirement: Monitor page provides forward and backward jog controls

The Monitor page SHALL display two distinct operator controls for forward and backward jogging. Both controls SHALL use the same visual style as the existing calibration controls and SHALL remain part of the existing Monitor-page control area.

#### Scenario: Operator sees both jog controls

- **WHEN** the Monitor page is displayed
- **THEN** the page shows one forward jog control and one backward jog control with labels that clearly distinguish the two directions

### Requirement: Forward jog is a momentary PLC command

The forward jog control SHALL resolve the PLC variable named `CmdJogFwd` and SHALL write boolean `true` while the control is actively pressed and boolean `false` when the press ends.

#### Scenario: Operator presses and releases forward jog

- **WHEN** the operator presses the forward jog control
- **THEN** the HMI writes `true` to the PLC address resolved from `CmdJogFwd`
- **WHEN** the operator releases the forward jog control or the control loses mouse capture
- **THEN** the HMI writes `false` to the PLC address resolved from `CmdJogFwd`

### Requirement: Backward jog is a momentary PLC command

The backward jog control SHALL resolve the PLC variable named `CmdJogBwd` and SHALL write boolean `true` while the control is actively pressed and boolean `false` when the press ends.

#### Scenario: Operator presses and releases backward jog

- **WHEN** the operator presses the backward jog control
- **THEN** the HMI writes `true` to the PLC address resolved from `CmdJogBwd`
- **WHEN** the operator releases the backward jog control or the control loses mouse capture
- **THEN** the HMI writes `false` to the PLC address resolved from `CmdJogBwd`

### Requirement: Jog controls preserve existing control behavior

Adding the jog controls SHALL NOT change the labels, styling, command semantics, or PLC points of the existing calibration and setup controls.

#### Scenario: Existing controls remain unchanged

- **WHEN** the Monitor page is used after the jog controls are added
- **THEN** the calibration and setup controls continue to use their existing momentary behavior and PLC variable names
