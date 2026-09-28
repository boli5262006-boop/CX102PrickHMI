# Design

## Context

`MonitorView.xaml` already places calibration and setup controls in one horizontal control group. Those controls attach `MomentaryButtonBehavior.Command` and use `HoldButtonStyle`. `MainViewModel` already exposes `IRelayCommand<bool>` hold commands and routes them through `SendMomentaryAsync`, which resolves a variable name to its PLC address and writes a boolean value. See `proposal.md` for the motivation and `specs/monitor-jog-buttons/spec.md` for the observable contract.

## Goals / Non-Goals

**Goals:**

- Reuse the existing button visual treatment and press/release event behavior.
- Add independent forward and backward command properties that send the requested PLC variable names.
- Ensure release and lost mouse capture both produce the false signal through the existing behavior.
- Keep the change localized to the Monitor control group and its owning command ViewModel.

**Non-Goals:**

- No new attached behavior, style, OPC UA dependency, polling mechanism, or PLC address format.
- No changes to jog speed, interlocks, machine-state validation, or command sequencing beyond the existing momentary-command path.
- No changes to existing calibration or setup commands.

## Decisions

### Reuse the existing momentary behavior

The two buttons will use `b:MomentaryButtonBehavior.Command` rather than ordinary `Button.Command`. This guarantees that a press sends `true`, while mouse release or lost capture sends `false`, matching the established safety behavior. Implementing separate mouse handlers would duplicate release edge cases and could leave a command active.

### Reuse `HoldButtonStyle`

Both controls will use `Style="{StaticResource HoldButtonStyle}"`, matching the calibration controls instead of introducing a near-duplicate style. The buttons will be inserted in the existing horizontal control group with the same spacing convention.

### Add ViewModel command properties beside existing hold commands

The owning ViewModel will expose `HoldJogFwdCommand` and `HoldJogBwdCommand` as boolean relay commands. Each command delegates to the existing momentary send method with exactly one PLC variable name: `CmdJogFwd` or `CmdJogBwd`. This keeps address lookup, write failure handling, and asynchronous execution consistent with the existing command family.

### Verify using source-level and build checks

Validation will confirm the XAML bindings reference the new command properties, the command methods use the exact PLC names, and the project builds without changing unrelated controls. If automated UI tests are unavailable, the hold behavior will be validated by inspecting the shared behavior path and running the available build checks.

## Risks / Trade-offs

- **[Risk] The configured PLC tag table does not contain one of the new variable names.** → The existing lookup failure path will report the command failure; deployment must add both names to the runtime PLC settings before commissioning.
- **[Risk] Two jog buttons may be pressed simultaneously.** → This design preserves the existing independent momentary-command semantics and does not invent a new interlock; any mutual exclusion requirement must be supplied by the PLC or specified separately.
- **[Risk] The control group becomes too wide at the target resolution.** → Keep the existing compact button sizing and spacing, then verify the Monitor page layout at the supported window size.

## Migration Plan

1. Add the two command properties and bindings.
2. Build and verify the Monitor page.
3. Confirm `CmdJogFwd` and `CmdJogBwd` exist in the deployed PLC point configuration before operating the controls.
4. Rollback is limited to removing the two button bindings and command properties; existing controls remain unchanged.
