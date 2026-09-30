# Design

Implementation design for `redesign-history-datetime-query`. Motivation and scope live in
[proposal.md](proposal.md); requirements live in [specs/history-datetime-query/spec.md](specs/history-datetime-query/spec.md).
This document records the HOW: exact merge points, control choices, validation math, command structure,
and dialog ownership. All API facts cited below were verified against the actual referenced artifacts
(`packages/HandyControl.3.5.1/lib/net472/HandyControl.dll`, current source files).

## Context

- `CX102PrickHMI.csproj` targets **.NET Framework 4.7.2** and already references **HandyControl 3.5.1**
  (csproj:52-53, `packages.config:7`). `CommunityToolkit.Mvvm 8.4.2` (`packages.config:4`) supplies
  `AsyncRelayCommand`. **No new dependency is available or needed.**
- The HandyControl DLL embeds its themes as BAML: `themes/skindark.baml`, `themes/skindefault.baml`,
  `themes/theme.baml` (enumerated from the DLL's `g.resources`). They are reachable at
  `pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml` and `…/Themes/Theme.xaml`.
  **No theme dictionary is merged today** — `styles/Generic.xaml` merges only app-owned
  `Colors.xaml` → `Controls.xaml` → `ScrollBars.xaml`, and `App.xaml` merges only `Generic.xaml`.
- `HandyControl.Controls.DateTimePicker` exists in 3.5.1 with `SelectedDateTime` (`DateTime?`),
  `DateTimeFormat` (string), `DisplayDateTime`, and a `SelectedDateTimeChanged` event.
  `HandyControl.Controls.Window` exists with themed chrome (`NonClientAreaContent`, close-button
  brushes, `ShowNonClientArea`).
- **Collision surface (verified):** HC's `Theme.xaml` declares *implicit* styles for many native types:
  `Button`, `TextBox`, `ScrollViewer` (`ScrollViewerNativeBaseStyle`), `ScrollBar`, `Slider`, `ToolTip`,
  `ContextMenu`, `ComboBox`, `CheckBox`, `TabControl`, `DatePicker`, etc. The app's own
  `styles/ScrollBars.xaml:93` declares an implicit `ScrollBar` style; every existing view sets explicit
  styles on its `Button`/`TextBox` instances (checked MainWindow, MonitorView, HistoryAlarmView,
  NumericKeypadOverlay), so today's visuals are shielded by explicit-style precedence — the merge
  order below preserves that.
- History query today (MainViewModel.cs:747-811): two free-text `StartDateText`/`EndDateText`
  (`yyyy-MM-dd`), calendar-day span approximation (`(endDate.Date - startDate.Date).TotalDays > 1`),
  synchronous half-open `IAlarmRepository.GetByTimeRange(startInclusive, endExclusive)` wrapped in
  `Task.Run` with `end = DateTime.Now` (pairing extension), in-memory pairing projection
  `BuildHistoryProjection` (untouched by this change), rejections via stock
  `System.Windows.MessageBox` (`RejectHistoryRange`, MainViewModel.cs:817).
- `MainViewModel` is the runtime `DataContext` of all four pages (constructor-injected singletons via
  `Configure/ConfigureService.cs`). `HistoryAlarmViewModel.cs` is dead code and stays untouched.
- `Views/NumericKeypadOverlay` is the precedent for themed in-window overlays, but it lives *inside*
  MainWindow's grid — it cannot appear centered over the app from a validation helper without
  re-parenting. The user explicitly asked for a small themed **DialogWindow**; `hc:Window` is the fit.
- `HoldButtonStyle` (`styles/Controls.xaml:140-158`) is the exact 校准外侧 look (hover `PrimaryLight`
  border, pressed `Primary` background + white text). `MonitorView.xaml:204-208` is its reference usage.

## Goals / Non-Goals

**Goals:**

- Merge HandyControl's dark theme through `styles/Generic.xaml` with zero visual regression on the
  four existing pages.
- Replace the free-text date boxes with two `hc:DateTimePicker` controls bound to `DateTime?`
  properties, prefilled to yesterday 00:00:00 / today 00:00:00.
- Enforce the exact 48-hour rule (`end - start > 48h` rejected, `== 48h` accepted) and half-open
  `[start, end)` window semantics, keeping the DB read extended to `now` for cross-window pairing.
- Add 近2小时 / 近6小时 quick-range buttons that set the pickers and immediately query.
- Restyle 查询 + both quick buttons with the existing `HoldButtonStyle`.
- Route validation rejections and query/DB failures to a small dark-themed modal
  `DialogWindow : hc:Window` with a single 确定 button; last successful results stay visible on failure.

**Non-Goals:**

- No export, alarm-level filtering, paging, new button styles, or pairing-algorithm changes
  (proposal Impact; spec 结果展示与配对投影保持不变).
- No changes to `AlarmService`, `IAlarmRepository`, PLC polling, `HistoryAlarmViewModel.cs`, or any
  other page's visuals.
- No app-wide skin-switching infrastructure (single hard-wired dark theme).

## Decisions

### D1 — Theme merge location and ordering (styles/Generic.xaml)

Final content of `styles/Generic.xaml` `MergedDictionaries` — **HC dictionaries first, app
dictionaries last**:

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml" />
    <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/Theme.xaml" />
    <ResourceDictionary Source="/CX102PrickHMI;component/styles/Colors.xaml" />
    <ResourceDictionary Source="/CX102PrickHMI;component/styles/Controls.xaml" />
    <ResourceDictionary Source="/CX102PrickHMI;component/styles/ScrollBars.xaml" />
</ResourceDictionary.MergedDictionaries>
```

Rationale:

- **Ordering semantics:** in a `MergedDictionaries` collection, the *last* dictionary wins
  same-key lookups. App-owned keys (`PageBackground`, `Surface…`, `Primary…`, `Ink…`, `Border`,
  `Error`…) do not collide with HC brush keys (`PrimaryBrush`, `RegionBrush`, `BorderBrush`,
  `PrimaryTextBrush`… — verified disjoint), but two ordering effects still matter and both favor
  HC-first:
  1. The app's implicit `ScrollBar` style (`ScrollBars.xaml:93`) must override HC's implicit
     `ScrollBar` style app-wide, including inside HC popups, or the custom thin scrollbars regress.
  2. HC's implicit native styles (`Button`, `TextBox`, `ScrollViewer`…) are then shadowed wherever the
     app declares its own implicit style; explicit per-element styles in all current views keep
     precedence regardless of order.
- **SkinDark over SkinDefault:** gives every HC control (DateTimePicker popup calendar/clock,
  DialogWindow chrome) a dark palette out of the box, satisfying "无默认浅色样式残留".
- **Accent alignment:** HC's own accent differs from the app's. Append a small, commented
  **HC-brush alias section to `styles/Colors.xaml`** (it merges after HC, so the aliases win):

  ```xml
  <!-- HandyControl 皮肤别名：让 HC 控件/对话框复用应用深色调色板 -->
  <SolidColorBrush x:Key="PrimaryBrush" Color="#0EA5E9" />
  <SolidColorBrush x:Key="DarkPrimaryBrush" Color="#0284C7" />
  <SolidColorBrush x:Key="RegionBrush" Color="#1A1D23" />
  <SolidColorBrush x:Key="SecondaryRegionBrush" Color="#232730" />
  <SolidColorBrush x:Key="BorderBrush" Color="#3A3F49" />
  <SolidColorBrush x:Key="SecondaryBorderBrush" Color="#3A3F49" />
  <SolidColorBrush x:Key="PrimaryTextBrush" Color="#F0F1F3" />
  <SolidColorBrush x:Key="SecondaryTextBrush" Color="#9CA3AF" />
  <SolidColorBrush x:Key="ThirdlyTextBrush" Color="#6B7280" />
  <SolidColorBrush x:Key="DangerBrush" Color="#EF4444" />
  ```

  This is the mechanism that makes the picker and dialog render in *application* colors, not just
  "HC dark". HC styles reference these keys via `DynamicResource`, so live aliasing works. The set is
  the minimal accent/surface/text/border core; visual QA (see Migration Plan) may extend it (e.g.
  `TitleBrush`, `TextIconBrush`) — trimming/adding aliases never changes behavior.

Alternatives considered: merging HC in `App.xaml` next to `Generic.xaml` (rejected — scatters theme
ownership across two files and puts HC *after* app dictionaries, breaking the ScrollBar override);
creating a new `styles/HandyControlTheme.xaml` wrapper (rejected — one indirection layer for two
pack URIs adds nothing); remapping *every* HC brush (rejected — high regression surface on HC-internal
contrast for zero requirement value).

### D2 — Small themed dialog: `Views/DialogWindow` subclassing `HandyControl.Controls.Window`

Create **`CX102PrickHMI/Views/DialogWindow.xaml` (+ .cs)**, a reusable modal message box:

- XAML root `<hc:Window>`, `Title="提示"`, `Width="360"`, `SizeToContent="Height"`,
  `ResizeMode="NoResize"`, `ShowInTaskbar="False"`, `WindowStartupLocation="CenterOwner"`,
  `FontFamily="{DynamicResource AppFont}"`. HC's chrome (title bar + close button) is themed dark by
  SkinDark + the D1 aliases — no chrome work needed.
- Body: wrapped message `TextBlock` (`Foreground=Ink`, `FontSize=14`) + one **确定** button
  (`PrimaryButtonStyle`, right-aligned) — no Cancel, per spec (单一"确定"按钮).
- Code-behind: a `Message` string property; `OnOkClick → Close()`; `PreviewKeyDown` Escape → `Close()`.
- Public factory used by the ViewModel:

  ```csharp
  public static void ShowInfo(string message, string title = "提示")
  {
      var window = new DialogWindow { Message = message, Title = title };
      var owner = Application.Current?.MainWindow;
      if (owner != null && owner.IsLoaded) { window.Owner = owner; }
      else { window.WindowStartupLocation = WindowStartupLocation.CenterScreen; }
      window.ShowDialog();
  }
  ```

- **Ownership/close behavior:** modal `ShowDialog()` owned by `Application.Current.MainWindow`
  (center-on-owner; CenterScreen fallback when no loaded owner, e.g. designer/tests). Closing happens
  via 确定, Escape, or the HC title-bar close button — all identical for an informational dialog. Both
  call sites (validation rejection, DB-failure catch) execute on the UI thread (the `await
  Task.Run(...)` continuation resumes on the dispatcher), so no marshaling is required.
- **No DI registration:** the dialog is transient and stateless; `ConfigureService` registers only
  long-lived singletons. Adding `IDialogService` was considered and rejected: the codebase's existing
  precedent is a direct `MessageBox.Show` in `RejectHistoryRange`, there is no existing dialog
  abstraction or test seam to honor, and a static factory keeps call sites one-liners. The factory
  name keeps the seam obvious if a service is ever extracted.
- **System `MessageBox` retires from this path only:** `RejectHistoryRange` switches to
  `DialogWindow.ShowInfo(...)`; `HistoryStatus` keeps its existing role (inline "查询失败，请稍后重试"
  on DB failure, empty on validation rejection). Other `MessageBox` uses elsewhere in the app are out
  of scope.

Alternative considered: `HandyControl.Controls.MessageBox.Show(...)` (themed already, zero new files).
Rejected because the user explicitly asked for a small DialogWindow; HC's MessageBox layout (button
set, title handling, sizing) is not controllable enough for the "single 确定, small card" requirement,
and a `hc:Window` subclass doubles as the reusable pattern for future HMI prompts.

### D3 — DateTimePicker bindings, defaults, and null handling

- ViewModel replaces `StartDateText`/`EndDateText` (MainViewModel.cs:721-733) with:

  ```csharp
  private DateTime? _historyStartTime;
  public DateTime? HistoryStartTime { get { ... } set { SetProperty(...); } }
  private DateTime? _historyEndTime;
  public DateTime? HistoryEndTime { get { ... } set { SetProperty(...); } }
  ```

- Constructor default (replacing MainViewModel.cs:109-111), per spec prefill — a 24 h span that
  trivially satisfies the 48 h rule, so an immediate 查询 succeeds:

  ```csharp
  var today = DateTime.Today;
  HistoryStartTime = today.AddDays(-1); // 前一日 00:00:00
  HistoryEndTime = today;               // 当日 00:00:00
  ```

- View binding (TwoWay, HC raises `SelectedDateTime` on picker close/text commit):

  ```xml
  <hc:DateTimePicker SelectedDateTime="{Binding HistoryStartTime, Mode=TwoWay}"
                     DateTimeFormat="yyyy-MM-dd HH:mm:ss" Width="190" />
  ```

  `DateTimeFormat` is set explicitly (default is date-only) so operators pick seconds.
- **Nullable handling:** HC can deliver `null` (text cleared / unparseable input). Validation treats
  null as a rejection ("请选择有效的起始与结束时间") *before* any comparison — never
  `Value`-dereferences an unset picker. All downstream code (`windowStart = start.Value`, projection
  filter, summary) runs only after validation guarantees non-null.

### D4 — Query flow: exact 48 h validation, half-open range, DB read to now

`QueryHistoryAsync()` keeps its shape but the validation core becomes (replacing
MainViewModel.cs:749-774 and `TryParseHistoryDate`):

```csharp
if (HistoryStartTime == null || HistoryEndTime == null)
    return RejectHistoryRange("请选择有效的起始与结束时间");
var start = HistoryStartTime.Value;
var end = HistoryEndTime.Value;
if (start > end)
    return RejectHistoryRange("起始时间不能晚于结束时间，请检查所选范围");
if (end - start > TimeSpan.FromHours(48))
    return RejectHistoryRange("查询跨度不能超过 48 小时，请缩小时间范围");
```

- Exactly 48 h passes (`>` strict) — spec 恰好 48 小时被接受.
- The calendar-day approximation (`(endDate.Date - startDate.Date).TotalDays > 1`) and
  `TryParseHistoryDate` are deleted; rejection dialogs now say 时间 not 日期.
- Rejection (`RejectHistoryRange`) = NLog warn (unchanged) + `DialogWindow.ShowInfo(reason)`;
  **no query, no mutation of `HistoryItems` / `DateSummary` / `HistoryStatus`**.
- DB read unchanged in mechanism: `await Task.Run(() => alarmservice.GetByTimeRange(start,
  DateTime.Now))` — repository stays synchronous half-open (`InsertTime >= start && InsertTime < end`,
  AlarmService.cs:22-32); the read end still extends to `now` so leaves after `end` pair with
  in-window arrivals. The arrival-window filter in `BuildHistoryProjection` now receives the exact
  `[start, end)` instants instead of day boundaries; the pairing algorithm itself is untouched.
- Success: results replace `HistoryItems` wholesale via the existing dispatcher.Invoke;
  `DateSummary = string.Format(CultureInfo.InvariantCulture, "共 {0} 条记录 · {1} 至 {2}",
  entries.Count, FormatHistoryTimestamp(start), FormatHistoryTimestamp(end))` (timestamps now
  `yyyy-MM-dd HH:mm:ss`); `HistoryStatus` cleared.
- DB failure: catch logs via NLog (unchanged), sets `HistoryStatus = "查询失败，请稍后重试"` (unchanged),
  **adds** `DialogWindow.ShowInfo("查询失败，请稍后重试")`; `HistoryItems` and `DateSummary` keep the
  last successful values (they were never touched before the throw).

### D5 — Quick-range commands and concurrency

```csharp
public IRelayCommand QuickRange2hCommand { get; }   // AsyncRelayCommand(QueryLast2HoursAsync)
public IRelayCommand QuickRange6hCommand { get; }   // AsyncRelayCommand(QueryLast6HoursAsync)

private async Task QueryLast2HoursAsync() => await QueryQuickRangeAsync(2);
private async Task QueryLast6HoursAsync() => await QueryQuickRangeAsync(6);

private async Task QueryQuickRangeAsync(double hours)
{
    var now = DateTime.Now;                       // 单次取时，保证 start <= end
    HistoryStartTime = now.AddHours(-hours);
    HistoryEndTime = now;
    await QueryHistoryAsync();                    // 共用 D4 校验+查询核心
}
```

- Quick ranges set both pickers (bindings refresh the visible values — spec 同步更新) and immediately
  execute the query; their spans (2 h / 6 h) always pass validation, so the "never rejected"
  requirement holds structurally.
- **Concurrency:** CommunityToolkit `AsyncRelayCommand` (8.4.2, default `AllowConcurrentExecutions =
  false`) auto-disables the clicked button while its query runs (same mechanism as the existing
  `QueryHistoryCommand`). Cross-command overlap (近2小时 clicked while a manual query is running)
  is closed by a single `bool _historyQueryRunning` guard at the top of `QueryHistoryAsync`:
  `if (!Monitor.TryEnter(_historyQueryRunning)) return;`-style check-and-set with try/finally release —
  a busy overlap silently no-ops (no dialog), and result application stays last-writer-only in one
  direction. This is simpler and safer than wiring three commands to one `IsBusy` property, and it
  preserves the existing "AsyncRelayCommand 运行期间自动禁用" behavior for the clicked button.

Alternative considered: deriving quick-range end lazily inside `QueryHistoryAsync` (rejected — the
spec requires the pickers to *display* the computed range, so the properties must be set up front).

### D6 — HistoryAlarmView layout with HoldButtonStyle

Filter row inside the existing `PanelStyle` border keeps its grid shape; columns become:

```
[日期范围] [hc:DateTimePicker start 190] [至] [hc:DateTimePicker end 190]
           [近2小时 HoldButtonStyle] [近6小时 HoldButtonStyle] [查询 HoldButtonStyle]
```

- `xmlns:hc="https://handyorg.github.io/handycontrol"` on the UserControl (verified XmlnsDefinition
  in the 3.5.1 assembly).
- All three buttons use `Style="{StaticResource HoldButtonStyle}"` — 查询 drops
  `PrimaryButtonStyle` (spec: 查询按钮与两个快捷按钮 SHALL 复用现有样式，不新建). Hover = `PrimaryLight`
  border, pressed = `Primary` background + white text: pixel-identical to 校准外侧
  (Controls.xaml:148-157).
- Margins follow the existing 10 px rhythm (`Margin="0,0,10,0"` on pickers/buttons) inside the 16 px
  padded panel; label/至 separators reuse existing `Ink2`/`Ink3` foregrounds. Column widths for the
  two pickers widen from `140` to `190` to fit `yyyy-MM-dd HH:mm:ss`.
- Label text stays 日期范围 and the summary/失败行 (HistoryStatus/DateSummary) layout is untouched.

### D7 — Dependency and platform posture (no new packages)

- HandyControl 3.5.1 net472 is already referenced and delivered (csproj HintPath); the theme merge
  consumes only embedded BAML from that DLL. `packages.config` and csproj package references are
  **not modified**.
- `CommunityToolkit.Mvvm 8.4.2` net472 already provides `AsyncRelayCommand`/`ObservableObject`.
- Costura.Fody embeds referenced assemblies into the single exe; `pack://application:,,,/HandyControl`
  URIs resolve once Costura's assembly resolver has loaded HandyControl, which happens at first WPF
  resource lookup — covered by the launch smoke test in the Migration Plan. (Lowest-risk ordering:
  the theme-merge commit is verified by a full app launch before any picker work lands.)

## Risks / Trade-offs

- **[HC implicit styles capture previously WPF-default controls app-wide]** → Verified every existing
  control carries an explicit style and the only app-level implicit style at stake (`ScrollBar`) is
  merged *after* HC, so it wins. Mitigation: theme merge lands as its own commit with a four-page
  visual smoke test before dependent work; regressions would show up as unstyled-control look
  changes, not behavior changes.
- **[HC brush aliases could flatten HC-internal contrast, e.g. pressed/hover states]** → The alias
  set is the minimal accent/surface/text/border core; picker popup and dialog are visually QA'd;
  fallback is trimming aliases (worst case: only `PrimaryBrush`/`DarkPrimaryBrush`), which never
  touches app behavior.
- **[Theme parse adds one-time startup cost]** → Two BAML dictionaries from an already-loaded
  assembly; negligible for this HMI (single MainWindow, no skin switching).
- **[Modal dialog blocks the UI thread on DB failure]** → The dialog shows only *after* the failed
  await resumes on the dispatcher and after results were confirmed untouched; blocking is the desired
  modal acknowledgment per spec, bounded by operator clicking 确定.
- **[Busy-guard silently no-ops an overlapping quick click]** → Accepted trade-off (no spurious
  dialogs, no interleaved result replacement); the clicked-while-disabled UX already communicates
  busy state for same-command repeats.
- **[HC picker delivers null / user-typed garbage]** → Validation rejects with an actionable dialog
  before any arithmetic; default prefill guarantees a valid first query.
- **[Clock skew between two `DateTime.Now` calls]** → Eliminated by the single `now` capture in
  `QueryQuickRangeAsync` (D5).

## Migration Plan

1. **Commit 1 — theme foundation:** `styles/Generic.xaml` merge (D1) + `styles/Colors.xaml` alias
   section. Build in VS, launch, and eyeball all four pages (nav, monitor keypad overlay, overlap
   gauges, history list incl. scrollbar look). Expected: pixel-identical except nothing yet — this
   commit is intentionally behavior-free.
2. **Commit 2 — dialog:** add `Views/DialogWindow.xaml/.cs` (D2). Build.
3. **Commit 3 — ViewModel:** swap string props → `DateTime?` props with defaults, rewrite validation
   to D4, add quick commands + busy guard (D5), route both message paths through
   `DialogWindow.ShowInfo`. Build.
4. **Commit 4 — view:** HistoryAlarmView XAML per D6. Build.
5. **QA checklist (maps 1:1 to spec scenarios):** prefill → direct 查询 succeeds; pick exact
   08:00:00/10:00:00 window incl. boundary rows (08:00:00 in, 10:00:00 out); cross-window pairing
   shows real recovery time; exactly-48 h accepted; +1 min rejected with dialog and list preserved;
   inverted range rejected; 近2小时/近6小时 update pickers and query immediately; force a DB failure
   (e.g. rename the SQLite file) → dialog shown, last results + summary retained, app alive; confirm
   stock light `MessageBox` never appears on the history path; hover/pressed look of all three
   buttons matches 校准外侧.
6. **Rollback:** each commit is independently revertible; reverting Commit 1 alone restores the
   pre-change look of the whole app. No DB schema, config-file, or package changes exist to unwind.

## Open Questions

None. Every behavior-affecting choice (merge order, dark skin, alias mechanism, dialog type and
ownership, default range, 48 h math, quick-range flow, concurrency guard, button styling) is pinned
above; the only QA-contingent item (alias-set breadth, D1) has a defined fallback and cannot change
behavior.
