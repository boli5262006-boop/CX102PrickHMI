# Design

## Context

WPF HMI (.NET Framework 4.7.2, old-style csproj + packages.config) with one `MainViewModel` (CommunityToolkit.Mvvm, manual `SetProperty`) bound as DataContext to all four page views (`MainViewModel.cs:72-75`). The alarm chain is broken in three places: `PlcDevice_AlarmTriggerEvent` (`MainViewModel.cs:843`) is a commented-out stub; `AlarmService` has only `Insert`; `HistoryAlarmView.xaml` is a hardcoded prototype (fixed dates, hardcoded 级别 columns, level ComboBox, 导出 button, fake "第 1 / 1 页" label). `AlarmItems`/`HistoryItems`/`DateSummary` already live on `MainViewModel`; per-page `AlarmListViewModel`/`HistoryAlarmViewModel` are dead code feeding only design-time `d:DataContext`.

Verified event contract (from `thinger.ConfigLib.dll`/`thinger.CommunicationLib.dll`): `DeviceBase.AlarmTriggerEvent(object sender, AlarmEventArgs e)` where `AlarmEventArgs = { IsTrigger: bool, AlarmNote, Name, CurrentValue, SetValue: string }`; `sender` is the `OPCUAVariable` (base `VariableBase`) carrying `VarName`, `HighAlarmEnable/HighAlarmValue/HighAlarmNote`, `LowAlarmEnable/LowAlarmValue/LowAlarmNote`. The event is raised synchronously from `DeviceBase.Update` inside the 250 ms `GetOPCUAValue` scan loop on a background thread — never on the UI thread. The library only raises events for variables with an alarm enable set; the handler still re-checks the flags defensively.

**Terminology mapping (user-facing alias, no runtime change):** 用户口径的 `IsMaxAlarm`/`IsMinAlarm` are conceptual aliases only; the runtime `Settings/settings.json` and `VariableBase` use `HighAlarmEnable`/`HighAlarmNote` (高限报警使能/说明) and `LowAlarmEnable`/`LowAlarmNote` (低限报警使能/说明). All artifacts of this change use the runtime field names; the alias mapping is documented once here and in the specs, never re-implemented.

Persistence: SQLite via SqlSugar (`data.db` next to the exe, connection string `sqlite` in App.config). Verified schema: `Alarms(InsertTime TEXT, Symbol TEXT, AlarmNote TEXT, AlarmState TEXT, VarName TEXT)`, all nullable, no primary key — exactly matches the model; no schema change. The shipped `data.db` currently holds ~15 legacy test rows. `styles/` chain is App.xaml → `Generic.xaml` → `Colors.xaml` + `Controls.xaml`; views consume keys via `StaticResource`. No DataGrid or ScrollBar styles exist. Requirements live in `specs/alarm-history/spec.md`; see proposal.md — Why for motivation.

## Goals / Non-Goals

**Goals:**
- Wire the full alarm lifecycle: 到达/离开 events → one `Alarms` row per event → live `AlarmItems` top-insert/remove with dedupe (rows show 状态=到达) → date-range history query (≤ 48 h) with pair-projected recovery times.
- Add read capability to `IAlarmRepository`/`AlarmService` without touching the generic `IRepository<T>`.
- Make both alarm views fully data-driven per the spec (drop the 级别 columns; live rows show 状态=到达; remove level filter/export/pagination; dialog-rejected invalid ranges) while keeping the existing ItemsControl table look.
- Codify the style-resource convention: single merge point, StaticResource consumption, csproj `Page` entry rule for any future dictionary.

**Non-Goals:**
- No schema change, no new NuGet dependencies, no changes to `settings.json` (read-only consumption), no PLC polling/heartbeat changes.
- No severity/level model: `Alarms` has no level column and the PLC exposes none; neither view shows a 级别 column (see D3).
- No live-list rebuild after restart, no alarm acknowledge/reset UI, no export, no interactive paging, no DataGrid or custom ScrollBar styling.
- No WAL/journal-mode tuning; no per-page ViewModel revival; dead ViewModels stay untouched.

## Decisions

### D1: `MainViewModel` stays the single runtime DataContext
All four pages already receive `MainViewModel` and the alarm collections/commands belong there (`AlarmItems`, `HistoryItems`, `DateSummary` exist today). New members: `StartDateText`/`EndDateText` (bound TextBoxes), `QueryHistoryCommand` (`AsyncRelayCommand`), `HistoryStatus` (inline text for query/DB failures only — validation rejection uses a dialog, see D7). `AlarmEntry` gains a `VarName` property (get-only, used for exact live-row matching; XAML templates unaffected). Only `d:DesignInstance` references in the two XAML files change to `MainViewModel`; the dead per-page ViewModels are not edited or deleted.
- *Why not*: separate AlarmList/HistoryAlarm ViewModels — would fight the established container-singleton pattern, force constructor/DI churn, and split alarm state across objects.

### D2: Event handler — guard, snapshot, persist synchronously, marshal UI via `Dispatcher.BeginInvoke`
`PlcDevice_AlarmTriggerEvent` replaces the stub:
1. Cast `sender` to `VariableBase`; ignore the event if `!HighAlarmEnable && !LowAlarmEnable` (disabled points never enter the lifecycle, per spec).
2. Derive content per the spec's enable-direction rule: `HighAlarmEnable → HighAlarmNote`, else `LowAlarmNote`; fallback chain for empty/null values: the other direction's note → `e.AlarmNote` → `VarName` → `"未命名报警"` — never null; normalize null→"" when notes are used as identity.
3. Capture `DateTime.Now` once per event (same timestamp for DB row and UI row) and persist exactly one `Alarms { InsertTime, AlarmState = "到达"|"离开" (from `e.IsTrigger`), AlarmNote, VarName, Symbol = null }` — append-only, leaves never modify earlier arrives; `Symbol` stays null per spec.
4. UI updates via `Application.Current.Dispatcher.BeginInvoke` (fire-and-forget, FIFO order preserved, polling thread never blocks on the UI pump): 到达 skips insertion if an entry with the same `(VarName, message)` is already present (repeat-arrive dedupe), otherwise `Insert(0, …)` so the newest row is on top; 离开 removes the first entry matching `(VarName, message)`.
- *Sync insert, not a background writer queue*: events are raised serially on the single polling thread, which already serializes writes and preserves chronological order; a SQLite insert is sub-millisecond and alarm transitions are rare — a `Channel`-based writer would add ordering/flush complexity for no measured bottleneck.
- *`BeginInvoke`, not `Invoke`*: `Invoke` from the polling thread risks stalling tag scans behind a busy UI; FIFO posting keeps add/remove order.
- *Failure policy*: every repository call wrapped in try/catch with `NLogHelper.Warn`. Insert failure logs and still updates the live list (live view must not depend on disk, and leave-removal from the live list happens regardless of whether the leave row persisted); the HMI never crashes or blocks the polling loop on DB faults.

### D3: No 级别 column in either view — live status text, no severity model
Neither view shows a 级别 column. The live list row shows 报警时间 / 报警内容 / 状态, where 状态 is the constant text `"到达"` (every live row is an active arrival; the row is removed on 离开, so no row can ever show anything else). The history list shows exactly three columns: 报警时间 / 报警内容 / 恢复时间. `Alarms` has no level column and the PLC exposes no severity, so there is nothing to bind or derive; the prototype's hardcoded 级别 columns and their uniform `"警告"` placeholder text are deleted, not wired up.
- *Why not*: High→错误/Low→警告 or 高限/低限 kind display (invents severity semantics the PLC does not provide); keeping a prototype 级别 column as unbound text (violates the data-driven goal); persisting a constant level to the DB (pointless writes; `Symbol` stays null per spec).

### D4: Repository gains one ranged query
`IAlarmRepository` adds `List<Alarms> GetByTimeRange(DateTime startInclusive, DateTime endExclusive)`; `AlarmService` implements it with `db.Queryable<Alarms>().Where(...).OrderBy(InsertTime).OrderBy(AlarmState).ToList()` under the existing per-call `SqlSugarClient`. The second `OrderBy(AlarmState)` is the deterministic tiebreak: at identical `InsertTime`, `"到达"` (U+5230) sorts before `"离开"` (U+79BB), which is the correct pairing order — no schema/index change needed. `IRepository<T>` stays untouched; no async API (net472 + sync SqlSugar; the VM wraps the call in `Task.Run`).
- *Why not*: adding `GetAll`/generic query builders to `IRepository<T>` (wider surface than any caller needs); async SqlSugar APIs (not available in the pinned 5.1.4.207 sync usage pattern here).

### D5: History pairing is an in-memory projection; event rows are never transformed
Query flow for 查询: validate (D7) → `Task.Run` → `GetByTimeRange(startDate, DateTime.Now)` (end extended to *now*, not the window end, so recovery times are truthful even when a leave falls after the queried day) → pair chronologically → filter → fill `HistoryItems` on the UI thread, newest 报警时间 first. Pairing: iterate rows in query order; per identity key `(Normalize(VarName), Normalize(AlarmNote))` keep a stack of open 到达 rows; each 离开 pops the most recent open arrive of the same identity → one episode (arrive→leave). This refines the spec's "同一点位变量名配对" with the note as a second key: behavior is identical for today's disjoint high/low-enabled variables, and correct for a hypothetical both-enabled variable where a high-limit leave must not recover a low-limit arrive. Episodes whose arrive falls inside the requested window are projected to `AlarmEntry(Time = arrive, Message = note, RecoveryTime = leave time or empty string when unpaired/still open)`. Orphan leaves (arrive before the window) are dropped.
- *Why not*: fetching only `[start, end)` (an alarm recovered the morning after the queried day would misleadingly show empty recovery); SQL-side pairing/window functions (SQLite + SqlSugar make this awkward, and pairing logic belongs in one testable place); mutating or re-writing DB rows on query (history must stay a raw append-only event log, per spec); pairing by `VarName` alone (mis-pairs when one variable carries both enables).
- *Duplicate identity edge*: same var+note re-triggering before the previous episode closes — the leave pairs with the latest open arrive; the older episode renders an empty recovery. Accepted and documented.

### D6: HistoryAlarmView rework — real bindings, controls removed, same table pattern
Remove the level ComboBox, the 导出 button, and the "第 1 / 1 页" label entirely (the spec removes pagination as a concept — no `PageText` property is added). Keep the three columns 报警时间 / 报警内容 / 恢复时间 — the prototype's hardcoded 级别 column is deleted, not bound (per D3) — reusing the existing header/cell styles. Bind: start/end `TextBox` (`InputStyle`) → `StartDateText`/`EndDateText`; 查询 `Button` (`PrimaryButtonStyle`) → `QueryHistoryCommand`; `ItemsSource` → `HistoryItems` (ScrollViewer shows everything, no paging); footer `DateSummary` → real `"共 N 条记录 · {start} 至 {end}"`; `HistoryStatus` shows query/DB failure text inline. Keep the `ItemsControl` + header-grid table pattern for visual consistency with AlarmListView.
- *Why not*: `DataGrid` (default Aero chrome needs heavy restyling on 4.7.2, breaks the table look, adds editing/virtualization behavior nobody asked for); keeping a computed 1/1 page label (spec explicitly removes fixed pagination text); MessageBox for DB/query failures (a modal dialog on a transient fault is hostile on a touch HMI — inline status + retry-by-requery, reserving dialogs for the spec-mandated validation case).

### D7: Date-range validation — dialog rejection per spec
`yyyy-MM-dd` invariant parse for both fields; all must hold: parse succeeds, start ≤ end, (end − start) ≤ 2 days (48 h, i.e. 2026-07-01…2026-07-02 is legal, +2026-07-03 is not). Window end used for the *filter* is `endDate.Date + 1 day` (end date inclusive, whole day). Any violation shows a modal confirmation dialog (`MessageBox.Show`, OK-only) with the reason (格式/倒置/超过48小时), logs via NLog, and skips the query; after confirming, the user edits the dates and re-queries. Success clears any prior `HistoryStatus`.
- *Why not*: inline-only error text (spec explicitly requires a 提示对话框 with user confirmation before re-querying); silent clamp of over-long ranges (hides the constraint from operators).

### D8: No startup rebuild of the live list — accepted limitation, explicitly decided
After an HMI restart, `AlarmItems` starts empty even if alarms were active at shutdown; they reappear on the next transition. All three planning artifacts (proposal, spec, existing views) are event-driven only, so this change does not seed the live list from persisted open episodes. If operators later report the gap, the D5 pairing already provides the data needed for a follow-up "open-episodes seeding" change.
- *Why not*: startup seeding (behavior beyond the spec; surfaces stale open episodes from lost leave-rows as phantom alarms; a scope decision this design is not entitled to make unilaterally).

### D9: Style resources — zero new dictionaries; convention codified
This change adds no resource dictionary: every needed key (`PanelStyle`, `InputStyle`, `PrimaryButtonStyle`, `TableHeaderTextStyle`, `TableCellTextStyle`, `MonoTableCellTextStyle`, color brushes) already resolves through App.xaml → `Generic.xaml` → `Colors.xaml`/`Controls.xaml`, and views must keep consuming them via `StaticResource` (styles internally use `DynamicResource` for colors, which stays the split of responsibilities). Therefore no csproj edit is expected from the styling work. The `style-resources` capability codifies the rule for *future* dictionaries: (1) new dictionaries merge under `styles/Generic.xaml` only — never per-view; (2) the old-style csproj requires an explicit `<Page Include>` + `MSBuild:Compile` entry for every new XAML dictionary, otherwise the build passes but resources fail silently at runtime; (3) no direct `Colors.xaml`/`Controls.xaml` merges from views. No default-ScrollBar restyle in this change (existing `ScrollViewer` look is accepted).
- *Why not*: adding a `DataGridStyles.xaml`/`ScrollBars.xaml` now (nothing consumes it — a dictionary with no consumer is dead weight and widens the csproj surface we explicitly want to keep minimal).

### D10: Database lifecycle and fault behavior
The shipped `data.db` already contains the `Alarms` table; nothing in the app creates it, so it remains a deployment artifact. If the file is missing, corrupted, or locked (e.g., opened by a DB tool), every insert/query throws into the D2 catch path: failures are logged, the live list keeps working, history shows the inline query-failure status — the HMI stays up. `SqlSugarHelper`'s per-call connection with `IsAutoCloseConnection` keeps long-lived locks off the table.
- *Why not*: `db.CodeFirst.InitTables<Alarms>()` at startup (silently changes DB lifecycle semantics and masks a broken deployment; the proposal expects no schema work).

## Risks / Trade-offs

- [DB write on the polling thread stalls a scan round if SQLite is slow/locked] → Bounded by rare alarm events and sub-ms inserts; failure path is try/catch + log, never a hang (no retry loop, no sync-over-async).
- [Insert failure loses a history row permanently while the live list still shows the alarm] → Logged as a warning with var/note/timestamp for post-hoc repair; live add/remove stays correct because it is independent of DB success.
- [HMI restart hides alarms that were active at shutdown until their next transition] → Accepted limitation (D8); history still contains the full event stream for audit.
- [`InsertTime` ties between arrive/leave of one identity] → `OrderBy(AlarmState)` tiebreak orders 到达 before 离开 deterministically; pairing then depends on scan order, not strict timestamps.
- [Both enables set with identical/empty note on one variable → the two logical alarms share one pairing/live identity] → Live dedupe and pairing treat them as one alarm; today's config (10 high-enabled, 3 low-enabled) shows no such collision; note-collision is a settings-authoring concern, documented in the spec.
- [Dispatcher flood if the PLC raises an event burst] → One `BeginInvoke` per event with FIFO order; 48 monitored variables and transition-only events make sustained floods implausible; `AlarmItems` stays small by construction (rows removed on leave, dedupe on repeat arrives).
- [Concurrent query during insert hits SQLite lock contention] → Caught and surfaced inline as 查询失败 with previous results kept; user retries. WAL/journal tuning explicitly out of scope.
- [Dev `data.db` contains ~15 legacy test rows] → Production images ship a clean `data.db`; dev/test DB can be reset manually. Do **not** delete `data.db` casually — nothing recreates the table (D10).
- [Modal validation dialogs can stack if the user re-clicks 查询 rapidly] → Validation runs before any await and `AsyncRelayCommand` disables re-entry while a query runs, so at most one dialog per click.

## Migration Plan

1. Implementation is code-only: `MainViewModel.cs`, `Services/AlarmService.cs`, `Interfaces/IAlarmRepository.cs`, `Models/AlarmEntry.cs`, `Views/AlarmListView.xaml`, `Views/HistoryAlarmView.xaml`; no csproj, App.config, settings.json, or schema edits are expected (any surprise need for a new dictionary triggers the D9 `Page`-entry rule).
2. Deploy by replacing the exe as usual; `data.db` ships unchanged — existing rows remain compatible (no column changes), legacy test rows are harmless.
3. Rollback = redeploy the previous build; no data migration to undo (the raw event log is forward/backward compatible with both versions).
4. QA pass with the PLC connected: trigger one high-limit and one low-limit alarm (verify top-insertion, dedupe on repeat arrive, removal on leave, live rows showing 状态=到达 with no 级别 column); verify 到达/离开 rows appear in `data.db` with empty `Symbol`; query the exact two end days (inclusive), a reversed range, and a 3-day span to verify inclusion and the rejection dialogs; stop/start the HMI mid-alarm to confirm the accepted D8 behavior; open `data.db` in an external tool during a query to confirm the inline failure path.

## Open Questions

None — all behavior-affecting choices (live 状态=到达 wording, no 级别 column, dialog rejection, pairing identity refinement, no-seeding restart limitation) are resolved above and consistent with `specs/alarm-history/spec.md`.
