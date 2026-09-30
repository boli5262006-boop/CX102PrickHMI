# Proposal

## Why

The history alarm page (历史报警) asks operators to type free-text `yyyy-MM-dd` dates into two TextBoxes. This forces calendar-day granularity even though the underlying repository stores full timestamps, so a query cannot target an exact window (e.g., the last 2 hours before a specific incident), and the current "two calendar days" validation is only an approximation of the intended 48-hour cap. In addition, input validation and query failures surface through the stock light-styled `MessageBox`, which breaks the application's dark theme on an HMI panel.

## What Changes

- Replace the two free-text date TextBoxes (`StartDateText` / `EndDateText`) in `HistoryAlarmView` with two `HandyControl.Controls.DateTimePicker` controls bound via `SelectedDateTime`, letting operators pick exact start/end date-times (including seconds).
- Enforce a precise 48-hour window: `end - start > 48h` (or `start > end`) is rejected, replacing the calendar-day-based span check.
- Add quick-range buttons for the recent 2 hours and recent 6 hours: each sets start = `now` minus its duration and end = `now`, then immediately executes the query — no separate query click needed.
- Restyle the query button and the two quick-range buttons with the existing `HoldButtonStyle` (`styles/Controls.xaml`), matching the look of the MonitorView 校准外侧 button; no new style is created.
- Show validation rejections and query failures in a small themed dialog (DialogWindow / HandyControl themed dialog) consistent with the current dark theme, instead of the stock `MessageBox` or an inline-only error text.
- Merge the HandyControl theme resource dictionaries through `styles/Generic.xaml` so the DateTimePicker and dialog render with the application's dark theme. HandyControl 3.5.1 is already referenced by the project (`packages.config` / csproj); no new dependency is added.
- Preserve existing behavior: the three-column results table (报警时间 / 报警内容 / 恢复时间), the in-memory arrival/leave pairing projection (orphan leaves dropped, still-active alarms shown with an empty recovery time), no export, no alarm-level filter, no paging, and the repository's half-open `[start, end)` range semantics — now applied to exact start/end times (start inclusive, end exclusive; the DB read still extends to the current moment so leaves occurring after the window can pair with in-window arrivals).

## Capabilities

### New Capabilities

- `history-datetime-query`: Exact date-time querying of historical alarms on the history page — DateTimePicker-driven start/end selection, precise 48-hour cap, recent 2h/6h quick ranges, themed error dialogs, and preservation of the existing results projection.

### Modified Capabilities

- None. The only existing spec, `overlap-splice-monitor`, covers the overlap monitor page (gauges, reset keypad, MonitorView buttons) and has no requirements touching the history alarm page.

## Impact

- **Affected code**:
  - `CX102PrickHMI/Views/HistoryAlarmView.xaml` — swap TextBoxes for HandyControl `DateTimePicker` controls, add 2h/6h quick buttons, restyle query/quick buttons with `HoldButtonStyle`.
  - `CX102PrickHMI/ViewModels/MainViewModel.cs` — replace `StartDateText`/`EndDateText` with start/end `DateTime` properties, rewrite `QueryHistoryAsync` validation (exact 48h), add quick-range commands, route rejection/failure messages to the themed dialog. `HistoryAlarmViewModel.cs` remains dead code and is not touched.
  - `CX102PrickHMI/styles/Generic.xaml` — merge HandyControl theme dictionaries (prerequisite for the themed controls); existing Colors/Controls/ScrollBars merges stay.
- **Dependencies**: HandyControl 3.5.1 already referenced (`packages.config:7`); the DLL exposes `HandyControl.Controls.DateTimePicker` with `SelectedDateTime` (`DateTime?`). No package changes.
- **Data access**: `AlarmService.GetByTimeRange(startInclusive, endExclusive)` half-open semantics are unchanged; only the caller passes exact timestamps instead of day boundaries.
- **Out of scope**: Export, alarm-level filtering, paging, new button styles, changes to the pairing algorithm, and any PLC/polling behavior.
