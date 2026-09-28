# Tasks

## 1. Add PLC momentary commands

- [x] 1.1 Add `HoldJogFwdCommand` and `HoldJogBwdCommand` beside the existing hold commands in `CX102PrickHMI/ViewModels/MainViewModel.cs`, delegating to `SendMomentaryAsync` with exactly `CmdJogFwd` and `CmdJogBwd`; verify the source contains both exact variable names and both commands accept the existing boolean press/release value.
- [x] 1.2 Confirm the existing `SendMomentaryAsync` path resolves both variable names and writes boolean values through the existing PLC lookup/write flow; verify the implementation reuses the existing failure feedback path without changing calibration or setup commands.

## 2. Add Monitor page controls

- [x] 2.1 Add forward and backward buttons to `CX102PrickHMI/Views/MonitorView.xaml` in the existing servo-control button group, using `MomentaryButtonBehavior.Command`, `HoldButtonStyle`, clear directional labels, and the established spacing pattern; verify both bindings resolve to the new command properties.
- [x] 2.2 Verify the two controls preserve the existing calibration and setup button declarations and do not introduce a new style or attached behavior; inspect the XAML diff for unrelated changes.

## 3. Validate the integrated change

- [x] 3.1 Build the solution with `dotnet build` from `F:\CX102PrickHMI` and verify the build completes successfully without new compiler or XAML errors.
- [x] 3.2 Verify the Monitor page behavior through source-level checks or available UI validation: pressing forward/backward sends `true`, releasing or losing capture sends `false`, and the exact PLC names are `CmdJogFwd` and `CmdJogBwd`.
