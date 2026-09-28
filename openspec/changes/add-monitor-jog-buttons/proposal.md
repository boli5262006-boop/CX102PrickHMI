# Proposal

## Why

The Monitor page already exposes hold-to-press calibration and setup controls, but it does not provide operators with dedicated forward and backward jog controls. Adding these controls will make manual positioning available from the same page while preserving the existing momentary PLC-command safety behavior.

## What Changes

- Add a forward jog button to `MonitorView`, labeled for forward movement and styled like the existing calibration buttons.
- Add a backward jog button to `MonitorView`, labeled for backward movement and styled like the existing calibration buttons.
- Bind the buttons to the existing momentary-button interaction pattern: write `true` while pressed and write `false` on release or lost mouse capture.
- Resolve and write the PLC command points by their variable names `CmdJogFwd` and `CmdJogBwd`.
- Preserve the existing calibration and setup controls and their behavior.

## Capabilities

### New Capabilities

- `monitor-jog-buttons`: Operator-facing forward and backward momentary jog controls on the Monitor page.

### Modified Capabilities

- None.

## Impact

- **Affected code**: `CX102PrickHMI/Views/MonitorView.xaml` for the two controls; `CX102PrickHMI/ViewModels/MainViewModel.cs` for the momentary command bindings and PLC writes, following the existing `HoldHomingInCommand`, `HoldHomingOutCommand`, and `HoldSetupCommand` pattern.
- **PLC integration**: Uses the existing variable-name-to-address lookup and boolean `WriteNodeAsync<bool>` path; the configured PLC point names must include `CmdJogFwd` and `CmdJogBwd`.
- **Out of scope**: New button styles, changes to the attached momentary behavior, changes to calibration/setup semantics, or changes to the PLC polling architecture.
