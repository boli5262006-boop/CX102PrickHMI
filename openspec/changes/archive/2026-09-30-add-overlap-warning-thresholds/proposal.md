# Proposal

## Why

The overlap/splice monitor page currently judges each of the three splice channels with a single binary threshold — `v < 2.0 || v > 6.0` (`MainViewModel.cs:240`) — so a reading is either fully NORMAL (green) or fully ALARM (red pulse). A value drifting toward the limit, e.g. 2.5 mm, is displayed identically to a healthy 4.0 mm until the moment it crosses into the red band, giving operators no early visual cue before an alarm. Adding an intermediate yellow warning band outside 4±1 mm (with the existing red alarm kept outside 4±2 mm) makes approach-to-limit visible in time to act, without changing when a true alarm fires.

## What Changes

- Replace the per-channel binary NORMAL/ALARM state (`SpliceValue1/2/3OutOfRange`) with a three-band state evaluated per 500 ms snapshot, priority red > yellow > green:
  - **Red alarm** (unchanged outer band): `v < 2 || v > 6` (outside 4±2 mm). Exact values 2.0 and 6.0 remain non-red.
  - **Yellow warning** (new): `2 <= v < 3 || 5 < v <= 6` (outside 4±1 mm but inside 4±2 mm). Exact values 3.0 and 5.0 remain non-warning, i.e. normal.
  - **Normal** (green): `3 <= v <= 5`.
- Add yellow warning visual variants to all affected outputs, preserving the existing red behavior for the outer band:
  - **Cards** (P-01 前段 / P-02 中段 / P-03 后段): border and pulse treatment gains a warning state between the current normal border and the red pulse animation.
  - **Corner accents**: Primary → Error switch gains an intermediate Warning stroke in the warning band.
  - **Badges**: `NORMAL`/`ALARM` badge gains a `WARN` state (Warning colors) in the warning band.
  - **Gauges**: `HslArcGauge` arc color Success/Error switch gains Warning in the warning band. Gauge scaling is untouched (`Min=0`, `Max=8`, `SettingValue=4`, `F1` format, target line at 4).
  - **Delta text**: the Δ text (relative to target 4.0, `MainViewModel.cs:242`) gains a warning marker and Warning color in the warning band; the existing alarm marker and Error color remain for the outer band.
  - **Global banner**: `HasAnySpliceAlarm`/`BannerText` (`MainViewModel.cs:245-248, 972-983`) gains a warning state (yellow, listing the 位号 in warning) shown when at least one channel is in warning and none is in alarm; the red pulse state and its `报警 | ALARM: <位号列表> 搭接量超出标准范围 (4±2mm)` text remain unchanged and take precedence.
- Recovery follows the same band edges in both directions: a channel returns red → yellow → normal (or enters yellow from normal) as values recross 2/3 and 5/6, matching the existing stateless per-snapshot evaluation — no latching or hysteresis is introduced.
- **No change to PLC or database alarm behavior.** Repository evidence confirms the splice band judgment is computed HMI-side from the polled `device.CurrentValue` snapshot (`MainViewModel.cs:227-248`) and drives only UI properties; the `Alarms` table insert / realtime alarm list is fed by the separate generic PLC `AlarmTriggerEvent` high/low alarm path (`MainViewModel.cs:1044+`), which this change does not touch. No PLC writes, no new tags, no schema changes.
- **No new dependency** and **no change to measurement scaling or target value** (range 0–8 mm, target 4 mm, ±0 delta baseline unchanged).

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `overlap-splice-monitor`: The existing binary over-limit judgment and linkage requirement (red outside 2..6, normal otherwise) becomes a three-band normal/warning/alarm state with the boundary semantics above; the constant two-state banner requirement gains a warning state between the green normal and red alarm states. Affected requirements in `openspec/specs/overlap-splice-monitor/spec.md`: `搭接超限判定与联动` and `搭接报警横幅常驻双态`.

## Impact

- **Affected code**: `CX102PrickHMI/ViewModels/MainViewModel.cs` — `UpdateSpliceSnapshot` (band evaluation, delta text), the `SpliceValue*` state properties (band state in addition to the existing `OutOfRange` flags), `RefreshSpliceBanner`, `HasAnySpliceAlarm`, and `BannerText`; `CX102PrickHMI/Views/OverlapMonitorView.xaml` — the three card templates (border pulse, corner accents, badge, gauge color, schematic highlight and value text, delta text) and the global banner triggers.
- **Affected spec**: `openspec/specs/overlap-splice-monitor/spec.md` (two requirements above move from binary to three-band behavior).
- **Not affected**: PLC points and polling architecture (500 ms snapshot unchanged), `CmdReset` pulse, numeric keypad behavior, settings button blink, alarm repository / `Alarms` table, HMI settings JSON, third-party dependencies (HslControls etc.), gauge range/target configuration.
