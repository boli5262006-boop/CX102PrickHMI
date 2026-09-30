# Design

## Context

The overlap page polls `device.CurrentValue` every 500 ms and maps three channels through `UpdateSpliceSnapshot` (`CX102PrickHMI/ViewModels/MainViewModel.cs:227-248`), which today evaluates one boolean band: `v < 2.0 || v > 6.0` → `SpliceValue1/2/3OutOfRange`, and builds `SpliceValue1/2/3Delta` as `(v - 4.0).ToString("+0.0;-0.0;0.0") + (outOfRange ? "超出标准" : "")`. `RefreshSpliceBanner` ORs the three flags into `HasAnySpliceAlarm`, whose setter re-raises `BannerText`; the banner getter renders green normal text or red `报警 | ALARM: …` text (`MainViewModel.cs:245-248, 959-983`).

`OverlapMonitorView.xaml` contains three hand-duplicated card templates (P-01/P-02/P-03). Every visual reacts to the per-channel bool via `Style.Triggers` `DataTrigger Binding="{Binding SpliceValueNOutOfRange}" Value="True"`: card border + pulse storyboard (`CardPulse1/2/3`), corner accents (`CornerStyle1/2/3`), NORMAL/ALARM badge, P-xx position badge, `HslArcGauge.GaugeColor`, schematic highlight/dimension/mm-value, and delta text. The banner has its own pulse storyboard (`BannerPulse`) bound to `HasAnySpliceAlarm`. All colors come from `styles/Colors.xaml` (`Success` #22C55E, `Warning` #F59E0B, `Error` #EF4444, `Primary`, `Ink3`); tinted backgrounds use inline alpha hex (`#1A…`, `#66…`, `#19…`, `#26…`).

Constraints carried into this design: 500 ms polling and the lock/snapshot pattern stay as-is; no PLC writes, tags, or `Alarms`-table path changes; no new NuGet dependency; red behavior (band edges, pulse animation, banner text shape) stays byte-compatible; no latching, hysteresis, or persistence. See proposal.md for motivation and the delta spec for the authoritative requirements.

## Goals / Non-Goals

**Goals:**

- A single, pure classification function producing exactly one of three bands per value, with the approved boundaries (red `v<2 || v>6`, yellow `2≤v<3 || 5<v≤6`, green `3≤v≤5`).
- Warning (yellow) visuals on all three cards, all their sub-elements, and the banner, inserted strictly between today's green and red states.
- Red precedence at card level (trigger ordering) and banner level (ViewModel aggregation), with recovery walking red → yellow → green across the same edges.
- Minimal churn to existing red triggers: the `SpliceValueNOutOfRange` bindings, pulse storyboards, and banner alarm path remain untouched.

**Non-Goals:**

- No deduplication/refactor of the three card templates into a shared template or user control (out of scope per proposal).
- No PLC/database alarm integration, no HMI settings JSON entries for the new thresholds (constants in code, same as the existing 2/6).
- No hysteresis, latching, event log, or acknowledgement flow.
- No changes to gauge range/target (`Min=0`, `Max=8`, `SettingValue=4`, `F1`), delta baseline 4.0, keypad, or `CmdReset` pulse.

## Decisions

### D1 — Add warning bools alongside the existing bools; classify via a private enum

Each channel keeps `SpliceValueNValue`, `SpliceValueNDelta`, `SpliceValueNOutOfRange` (semantics unchanged: `true` = red band only) and gains `SpliceValueNWarning` (`true` = yellow band). A private enum is the single classification result; the two bools are projections of it:

```csharp
internal enum SpliceBand { Normal, Warning, Alarm }

private static SpliceBand ClassifySplice(double v)
{
    if (v < 2.0 || v > 6.0) return SpliceBand.Alarm;    // outside 4±2, endpoints NOT alarm
    if (v < 3.0 || v > 5.0) return SpliceBand.Warning;  // outside 4±1 but inside 4±2, endpoints NOT warning
    return SpliceBand.Normal;                           // 3..5 inclusive
}
```

In `UpdateSpliceSnapshot`, after the existing parse, the enum is computed once and both flags are assigned from it within the same snapshot callback (Warning first, then OutOfRange), so the two bools can never diverge in steady state and no intermediate render is produced (WPF renders after the callback completes, not between the two `PropertyChanged` raisings).

Alternatives considered:

- *Replace the bools with one public `SpliceValueNBand` enum property.* Rejected: WPF `DataTrigger` compares by converted string (`Value="Warning"` works, but is less obvious to maintainers of this HMI codebase), and it would force edits to all ~20 existing red triggers and both storyboards — exactly the behavior the change must not alter — instead of only adding new triggers. Keeping `SpliceValueNOutOfRange` verbatim preserves the red path byte-for-byte.
- *Three extra independent bools without a shared classifier.* Rejected: two booleans per channel evaluated separately invite divergent edge conditions; the enum makes exclusivity structural and keeps the boundary logic in one testable place.

### D2 — Boundary semantics, precision, and missing/invalid handling

- Strict inequalities chain exactly as written above: 2.0 and 6.0 are warning (not alarm), 3.0 and 5.0 are normal (not warning). This matches the delta spec's edge scenarios.
- Values arrive as `float.TryParse` output widened to `double`. All threshold literals (2.0/3.0/5.0/6.0) are exactly representable in float, so boundary comparisons are exact; non-boundary values (2.01, 5.01) fall unambiguously inside their bands regardless of float rounding. The existing `v < 2.0 || v > 6.0` style is retained rather than switching to epsilon comparisons, because PLC-reported exact-threshold values must classify per spec and epsilon would move the edges.
- Missing key or unparseable value: `UpdateSpliceSnapshot` already returns early without touching any property; this behavior is kept, so a channel retains its last value/delta/band. Initial state before the first valid sample is all-false = Normal, identical to today.
- Boundary matrix (each value × each of the three channels must yield):

| v | Band | Delta text |
|---|------|-----------|
| 1.99 | Alarm | `+x.x 超限` per sign (e.g. `-2.1 超限`) |
| 2.0 | Warning | e.g. `-2.0 预警` |
| 2.01 | Warning | `-1.9 预警` |
| 3.0 | Normal | `-1.0` |
| 4.0 | Normal | `0.0` |
| 5.0 | Normal | `+1.0` |
| 5.01 | Warning | `+1.0 预警` |
| 6.0 | Warning | `+2.0 预警` |
| 6.01 | Alarm | `+2.0 超限` |
| key absent / unparseable | previous band retained | previous text retained |

Because `ClassifySplice` is a pure static function, this matrix is directly assertable; the repo currently has no test project, so tasks.md decides between adding a minimal test project and scripted manual verification — the design only guarantees the function is isolated enough for either.

### D3 — Delta text: one suffix per band, alarm wording changes per spec

Delta strings are built in `UpdateSpliceSnapshot` from the same enum value:

```csharp
var suffix = band == SpliceBand.Alarm ? " 超限" : band == SpliceBand.Warning ? " 预警" : "";
assignDelta((v - 4.0).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + suffix);
```

Note the deliberate, spec-mandated change to the alarm wording: today's `+3.3超出标准` becomes `+3.3 超限` (added space, shortened marker) to match the delta spec's scenario strings; normal text stays the bare signed delta (`-0.1`, `0.0`).

### D4 — Banner: red precedence computed in the ViewModel, yellow aggregation as one derived flag

Keep `HasAnySpliceAlarm = OR(SpliceValueNOutOfRange)` unchanged — it continues to drive the existing red pulse storyboard and red trigger untouched. Add `HasAnySpliceWarning` with precedence folded in, evaluated in `RefreshSpliceBanner` after the channel updates:

```csharp
var anyWarning = SpliceValue1Warning || SpliceValue2Warning || SpliceValue3Warning;
HasAnySpliceAlarm   = SpliceValue1OutOfRange || SpliceValue2OutOfRange || SpliceValue3OutOfRange;
HasAnySpliceWarning = anyWarning && !HasAnySpliceAlarm;
```

`HasAnySpliceWarning`'s setter follows the existing `HasAnySpliceAlarm` pattern and also raises `OnPropertyChanged(nameof(BannerText))`. `BannerText` gains one middle branch; the red branch keeps listing alarm 位号 only (spec: red text omits warning-only channels):

```csharp
if (HasAnySpliceAlarm)
    return "报警 | ALARM: " + Join(out-of-range 位号) + " 搭接量超出标准范围 (4±2mm)";
if (HasAnySpliceWarning)
    return "预警 | WARNING: " + Join(warning 位号) + " 搭接量接近标准范围 (4±1mm)";
return "检测正常 | SYSTEM NORMAL";
```

Alternative considered — exposing raw warning count/OR and letting XAML `MultiDataTrigger` on "warning && !alarm": rejected because it duplicates the precedence rule in ~6 banner trigger locations instead of once in the ViewModel, and the setter-raising pattern already exists for exactly this kind of derived text.

### D5 — XAML strategy: yellow warning triggers inserted before the red trigger in every style; yellow mirrors the red border pulse with Warning color

> 修订（用户确认）: 预警由静态黄色改为黄色脉冲呼吸，节奏与红色报警一致、颜色为 Warning。以下原决策中的"static/no storyboard"按本修订理解。

Per card (applied identically to P-01/P-02/P-03 with their per-channel bindings):

- **Card border**: `DataTrigger` on `SpliceValueNWarning=True` sets `BorderBrush={StaticResource Warning}` plus a yellow `DropShadowEffect` (`Color=#F59E0B`, `BlurRadius=40`, `Opacity=0.55`, `ShadowDepth=0`), with `EnterActions` beginning `CardPulseWarningN` (0.8s auto-reverse loop: border color `#F59E0B`→`#66F59E0B`, effect opacity `0.55`→`0.25`, blur `40`→`16`) and `ExitActions` stopping it — mirroring the red `CardPulseN` cadence exactly, Warning-colored. The existing red `DataTrigger` (setters + `CardPulseN`) stays untouched after it; red remains last-active-wins. Per-channel band exclusivity (Warning vs OutOfRange are mutually exclusive in the ViewModel) guarantees the two storyboards never run concurrently on one card.
- **Corner accents (`CornerStyleN`)**: warning trigger sets `Stroke={StaticResource Warning}`; red trigger unchanged.
- **Status badge**: warning trigger sets border/background to yellow tints and, on the inner `TextBlock`, `Text="WARN"` + `Foreground={StaticResource Warning}` (mirroring how the red trigger sets `Text` + `Foreground` in one trigger). Tint hex follows the file's existing alpha convention: `Background=#1AF59E0B`, `BorderBrush=#66F59E0B` (same alpha tiers as the green `#1A22C55E`/`#4D22C55E` and red `#26EF4444`/`#66EF4444` pairs); no `Colors.xaml` edits.
- **P-xx position badge**: warning trigger sets `Background=#19F59E0B`, `BorderBrush=#66F59E0B` on the badge border and `Foreground={StaticResource Warning}` on the P-xx label (replacing today's Primary → Error pair with Primary → Warning → Error).
- **Gauge**: `GaugeColor` warning trigger → `{StaticResource Warning}`; `SettingGaugeColor` target line, `Min/Max/SettingValue/format` untouched.
- **Schematic**: highlight rectangle warning trigger sets `Fill=#1AF59E0B` + `Stroke={StaticResource Warning}`; dimension path and mm-value `TextBlock` warning triggers set `Stroke`/`Foreground` to `{StaticResource Warning}`.
- **Delta text**: warning trigger sets `Foreground={StaticResource Warning}`; the text change itself comes from the bound `SpliceValueNDelta` string, no XAML text logic.

Banner: `DataTrigger` on `HasAnySpliceWarning=True` sets `BorderBrush={StaticResource Warning}` + yellow `DropShadowEffect` (`BlurRadius=20`, `Opacity=0.5`) on the banner border with `EnterActions` beginning `BannerWarningPulse` (0.8s auto-reverse: border color `#F59E0B`→`#66F59E0B`, effect opacity `0.5`→`0.2`) and `ExitActions` stopping it; dots and text keep static Warning setters. `HasAnySpliceWarning` is already masked by any alarm in the ViewModel, so `BannerWarningPulse` and `BannerPulse` never run simultaneously.

**Trigger ordering rule (the red-precedence mechanism at card level):** in every `Style.Triggers` collection touched, the warning trigger is inserted *before* the existing alarm trigger. WPF applies the last matching trigger last, so even in the impossible steady state where both bools are true (or any future regression in D1's exclusivity), red wins. Yellow pulse storyboard names (`CardPulseWarning1/2/3`, `BannerWarningPulse`) are distinct from the red ones (`CardPulse1/2/3`, `BannerPulse`), which stay unchanged.

### D6 — Everything else deliberately unchanged

500 ms timer cadence, `ValueLock` snapshot reads, `UpdateSpliceSnapshot`'s parse/early-return shape, `CmdReset`, alarm repository path, HMI settings, dependencies, and gauge configuration are untouched. The only VM surface changes are: one enum + one static function, three new bool properties, one new banner bool, extended `RefreshSpliceBanner`/`BannerText`, and the delta suffix switch inside `UpdateSpliceSnapshot`.

## Risks / Trade-offs

- [XAML duplication grows: ~12 new trigger blocks across three near-identical cards] → Accepted per Non-Goals; the insert-before-alarm ordering rule is stated once here and verified per card in tasks. A shared template remains the right future refactor if a fourth card ever appears.
- [User-visible delta wording change (`超出标准` → ` 超限`)] → Spec-mandated; called out in D3 so tasks/QA check both delta and banner strings rather than assuming red text is byte-identical.
- [Two bools per channel could diverge if a future edit assigns one without the other] → Both are written only from the `ClassifySplice` result inside one method; reviewers should reject any direct assignment to `SpliceValueNWarning`/`OutOfRange` elsewhere.
- [Float→double rounding near thresholds] → All thresholds are exactly float-representable and compared with strict inequalities, so only a genuinely-off-edge value can flip a band; matrix in D2 pins the exact literals.
- [Red→yellow transition interacts with the running storyboard] → Within one snapshot callback `Warning` is set first, `OutOfRange` second; the red trigger's `ExitActions` `StopStoryboard` runs as that trigger deactivates and the warning trigger's static yellow border applies in the same render pass. Verify visually once per card in tasks (stop pulsing, static yellow border, no residual glow).
- [Banner flaps between states while values hover on an edge] → Inherent to stateless per-snapshot evaluation required by the spec (no hysteresis); identical to existing red-edge behavior, just with one more state.

## Migration Plan

1. ViewModel first: add `SpliceBand`, `ClassifySplice`, the three `SpliceValueNWarning` properties, `HasAnySpliceWarning`, and extend `UpdateSpliceSnapshot`/`RefreshSpliceBanner`/`BannerText`. Compile and sanity-check the delta/banner strings against the D2/D3/D4 tables (cards still render two-state until XAML catches up).
2. XAML second: insert the warning triggers (D5) card by card — border, corners, badge, position badge, gauge, schematic, delta — then the banner. Per card, confirm the warning trigger sits above the alarm trigger in each `Style.Triggers`.
3. Verification pass: walk the D2 boundary matrix (forced values via simulation or temporary test harness), plus the spec scenarios for warning entry/recovery, red→yellow recovery, and red-over-yellow banner precedence.
4. Rollback: single git revert of the two files; there is no persisted state, schema, PLC, or configuration migration, so rollback and redeploy are the same operation.
