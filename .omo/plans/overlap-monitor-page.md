# 搭接量监控页面（静态画面）工作计划

## TL;DR

> **Quick Summary**: 基于 `D:\material_overlap_monitor.html` 在 CX102PrickHMI 中新增第二个监控页面"搭接量监控"。本里程碑只做静态画面（所有数值硬编码在 XAML，不做数据绑定），配色对齐本地主题。导航第二个重复的"刺辊监控"按钮改为"搭接量监控"。
>
> **Deliverables**:
> - `Views/OverlapMonitorView.xaml` + `.xaml.cs`（静态三卡片仪表盘页面）
> - `ViewModels/OverlapMonitorViewModel.cs`（空壳，仅用于导航/模板映射）
> - 导航：刺辊监控 / 搭接量监控 / 报警记录 / 历史报警
> - App.xaml 模板映射 + csproj 条目
>
> **Estimated Effort**: Medium
> **Parallel Execution**: NO - sequential（单页面特性，文件相互依赖）
> **Critical Path**: Task 1 → Task 2 → Task 3 → Task 4 → F1-F4

---

## Context

### Original Request
用户提供 `D:\material_overlap_monitor.html`（已完整读取，659 行），要求：
1. 新增"搭接量监控"页面，显示在 `<ContentControl Grid.Row="2" Content="{Binding CurrentPage}" />` 中；
2. 导航栏中重复的"刺辊监控"按钮之一改为"搭接量监控"；
3. 页面背景色必须与本地项目主题一致；
4. **只做画面，不做数据绑定**（所有数值硬编码）。

### Interview Summary
- 页面范围：三张仪表卡片（P-01 前段 4.2mm 正常 / P-02 中段 7.3mm 报警 / P-03 后段 3.8mm 正常）+ 报警横幅 + 重置按钮 + 页脚信息条。
- 不包含：实时数据、定时模拟、重置按钮命令逻辑（仅外观）。

### 颜色映射（HTML → 本地主题，必须遵守）
| HTML 变量 | 本地资源/值 |
|---|---|
| `--bg-base #1A1D21` | `PageBackground #0F1115` |
| `--bg-panel #24282E` | `Surface #1A1D23` |
| `--bg-panel-light` | `Surface2 #232730` |
| `--amber #0EA5E9` | `Primary #0EA5E9` |
| `--warn-red #EF4444` | `Error #EF4444` |
| `--ok-green #22C55E` | `Success #22C55E` |
| `--text-primary #E5E7EB` | `Ink #F0F1F3` |
| `--text-secondary` | `Ink2 #9CA3AF` |
| `--text-dim` | `Ink3 #6B7280` |
| `--border-color #374151` | `Border #3A3F49` |
| 网格线 `rgba(55,65,81,.12)` | 画刷 `#1F374151`（DrawingBrush 平铺 40px） |

中文/数值字体沿用项目全局 `AppFont`；数值读数与刻度用 `Consolas`。

---

## Work Objectives

### Core Objective
新增一个静态"搭接量监控"页面，可通过导航按钮切换显示于 ContentControl，视觉还原 HTML 布局且配色与本地主题一致。

### Concrete Deliverables
- `CX102PrickHMI/ViewModels/OverlapMonitorViewModel.cs`
- `CX102PrickHMI/Views/OverlapMonitorView.xaml` + `CX102PrickHMI/Views/OverlapMonitorView.xaml.cs`
- `MainWindow.xaml` 导航第 2 个按钮改为"搭接量监控"（ShowOverlapCommand + OverlapNavButtonStyle）
- `App.xaml` 增加 OverlapMonitorViewModel → OverlapMonitorView 的 DataTemplate
- `styles/Controls.xaml` 增加 `OverlapNavButtonStyle`
- `CX102PrickHMI.csproj` 增加对应 Compile/Page 条目

### Definition of Done
- [ ] `dotnet build "CX102PrickHMI.sln" --no-restore` → Build succeeded, 0 Error
- [ ] 启动 `CX102PrickHMI\bin\Debug\CX102PrickHMI.exe` 运行 ≥7s 无未处理异常
- [ ] 四个导航按钮切换正常，"搭接量监控"页在 ContentControl 中渲染三卡片

### Must Have
- 三张卡片：P-01(4.2, 正常)、P-02(7.3, 报警)、P-03(3.8, 正常)，含仪表盘（刻度/指针/读数）、搭接示意图（材料A/B + 重叠区 + 尺寸线）、数据脚注（标准 4±2mm / 偏差）
- 报警横幅：红色边框 + 双警灯 + 文案"报警 | ALARM: P-02 中段搭接量超出标准范围 (4±2mm)"，0.8s 呼吸闪烁
- P-02 卡片边框红色呼吸闪烁（0.8s，BorderBrush 颜色 + DropShadow 透明度/模糊）
- 重置按钮"重置报警 / RESET ALARM"（仅外观，无命令）、页脚信息条
- 页面背景 = PageBackground + 40px 网格纹理（DrawingBrush）

### Must NOT Have (Guardrails)
- 不做任何 `{Binding ...}` 数据绑定（除导航命令本身）
- 不写任何 C# 业务逻辑/定时器；OverlapMonitorViewModel 保持空壳
- 不接入设备/后台服务
- 不修改现有 MonitorView / AlarmListView / HistoryAlarmView 及其 ViewModel
- 不新增 NuGet 依赖
- 禁止把页面主体 XAML 写进 App.xaml（只允许 DataTemplate 类型映射）

---

## Verification Strategy

> **ZERO HUMAN INTERVENTION** - 全部由执行代理验证。

### Test Decision
- Infrastructure exists: NO（仓库无测试工程）
- Automated tests: None
- Agent-Executed QA: 必须 —— 构建 + 实际启动进程 + 存活检查 + 输出/错误捕获

### QA Policy
每个任务附 QA 场景，证据输出到 `.omo/evidence/`。UI 静态页无法 Playwright，验证方式为：构建成功 + 进程存活 + stderr 无异常 + （可选）用户人工截图比对。

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (sequential - 同一特性强耦合):
├── Task 1: ViewModel/导航/App 映射/Controls 样式 [quick]
├── Task 2: OverlapMonitorView 静态页面 XAML [deep]
├── Task 3: csproj 条目 [quick]
└── Task 4: 构建 + 启动验证 [quick]

Wave FINAL:
├── Task F1: 计划合规审计 (oracle)
├── Task F2: 代码质量审查 (unspecified-high)
├── Task F3: 实际运行 QA (unspecified-high)
└── Task F4: 范围保真检查 (deep)
-> 汇报结果 -> 用户确认
```

### Dependency Matrix
- 1: - → 2, 4
- 2: 1 → 3, 4
- 3: 2 → 4
- 4: 1,2,3 → F1-F4

### Agent Dispatch Summary
- 1: quick；2: deep；3: quick；4: quick；FINAL: oracle / unspecified-high / unspecified-high / deep

---

## TODOs

- [x] 1. 导航与页面接线（ViewModel / App 映射 / Controls 样式 / MainWindow 按钮）

  **What to do**:
  1. 新建 `CX102PrickHMI/ViewModels/OverlapMonitorViewModel.cs`：
     ```csharp
     using CommunityToolkit.Mvvm.ComponentModel;
     namespace CX102PrickHMI.ViewModels
     {
         public sealed class OverlapMonitorViewModel : ObservableObject
         {
         }
     }
     ```
  2. `ViewModels/MainViewModel.cs`：
     - 枚举 `HmiPage` 增加 `Overlap,`（放在 `Monitor,` 之后）；
     - 构造函数增加 `Overlap = new OverlapMonitorViewModel();` 和
       `ShowOverlapCommand = new RelayCommand(() => SelectPage(HmiPage.Overlap, Overlap));`
       （放在 ShowMonitorCommand 之后）；
     - 属性区增加 `public OverlapMonitorViewModel Overlap { get; }`、`public ICommand ShowOverlapCommand { get; }`；
     - 增加 `public bool IsOverlapSelected { get { return SelectedPage == HmiPage.Overlap; } }`，
       并在 `SelectedPage` setter 的 `SetProperty` 成功分支中追加
       `OnPropertyChanged(nameof(IsOverlapSelected));`。
  3. `styles/Controls.xaml`：在 `MonitorNavButtonStyle` 之后新增
     ```xml
     <Style x:Key="OverlapNavButtonStyle" TargetType="Button" BasedOn="{StaticResource NavButtonStyle}">
         <Style.Triggers>
             <DataTrigger Binding="{Binding IsOverlapSelected}" Value="True">
                 <Setter Property="Background" Value="{DynamicResource Primary}" />
                 <Setter Property="Foreground" Value="White" />
             </DataTrigger>
         </Style.Triggers>
     </Style>
     ```
  4. `App.xaml`：在 MonitorViewModel 模板之后新增
     ```xml
     <DataTemplate DataType="{x:Type vm:OverlapMonitorViewModel}">
         <views:OverlapMonitorView />
     </DataTemplate>
     ```
  5. `MainWindow.xaml` 导航 StackPanel（当前第 61-66 行，第 2 个按钮是重复的"刺辊监控"）改为四键：
     ```xml
     <Button Content="刺辊监控" Command="{Binding ShowMonitorCommand}" Style="{StaticResource MonitorNavButtonStyle}" />
     <Button Content="搭接量监控" Command="{Binding ShowOverlapCommand}" Style="{StaticResource OverlapNavButtonStyle}" />
     <Button Content="报警记录" Command="{Binding ShowAlarmsCommand}" Style="{StaticResource AlarmsNavButtonStyle}" />
     <Button Content="历史报警" Command="{Binding ShowHistoryCommand}" Style="{StaticResource HistoryNavButtonStyle}" />
     ```

  **Must NOT do**: 不改现有 Monitor/Alarms/History 页面与其 ViewModel；不给 OverlapMonitorViewModel 添加任何成员。

  **Recommended Agent Profile**: `quick`（多点小改动，规格已完全给出）。Skills: [`dotnet-wpf`]。

  **Parallelization**: NO；Blocks: 2、4；Blocked By: None。

  **References**:
  - `CX102PrickHMI/ViewModels/MainViewModel.cs:8-13,20-32,40-64` — 现有枚举/构造/属性模式，照抄同一风格
  - `CX102PrickHMI/styles/Controls.xaml:69-91` — Monitor/Alarms/History NavButtonStyle 的 DataTrigger 写法
  - `CX102PrickHMI/App.xaml:12-20` — 现有 DataTemplate 映射
  - `CX102PrickHMI/MainWindow.xaml:60-67` — 待修改的导航按钮区
  - WHY: 执行者没有会话上下文，以上给出确切行号与完整代码片段。

  **Acceptance Criteria**:
  - [ ] `dotnet build "CX102PrickHMI.sln" --no-restore` 编译通过（此任务后 OverlapMonitorView 尚不存在，App.xaml 模板会引用它 —— 因此 Task 1 与 Task 2 必须在同一批执行后再构建；本任务验收与 Task 2 合并执行构建）

  **QA Scenarios (MANDATORY)**:
  ```
  Scenario: 导航接线静态检查
    Tool: Bash (PowerShell)
    Steps:
      1. Select-String -Path "CX102PrickHMI\MainWindow.xaml" -Pattern "搭接量监控"
      2. Select-String -Path "CX102PrickHMI\ViewModels\MainViewModel.cs" -Pattern "ShowOverlapCommand|IsOverlapSelected|HmiPage.Overlap"
      3. Select-String -Path "CX102PrickHMI\styles\Controls.xaml" -Pattern "OverlapNavButtonStyle"
    Expected Result: 三处均命中；MainWindow 中"刺辊监控"仅出现 1 次
    Failure Indicators: 任一模式未命中；刺辊监控出现 2 次
    Evidence: .omo/evidence/task-1-nav-wiring.txt
  ```

  **Commit**: NO（与 2、3 合并提交）

- [x] 2. 静态页面 OverlapMonitorView.xaml（完整画面还原）

  **What to do**:
  1. 新建 `CX102PrickHMI/Views/OverlapMonitorView.xaml.cs`：
     ```csharp
     using System.Windows.Controls;
     namespace CX102PrickHMI.Views
     {
         public partial class OverlapMonitorView : UserControl
         {
             public OverlapMonitorView() { InitializeComponent(); }
         }
     }
     ```
  2. 新建 `CX102PrickHMI/Views/OverlapMonitorView.xaml`，结构如下（全部静态、零数据绑定）：
     - 根：`ScrollViewer VerticalScrollBarVisibility="Auto"` → `Grid Margin="24,0,24,24"`，行：Auto(标题)/Auto(横幅)/Auto(三卡片)/Auto(重置)/Auto(页脚)；
     - 页面标题行：`搭接量监控`(FontSize 20 SemiBold, Ink) + 小字 `MATERIAL OVERLAP MONITOR`(FontSize 11, Ink3)；
     - UserControl.Resources 内定义：
       - 网格纹理画刷：
         ```xml
         <DrawingBrush x:Key="PageGridBrush" TileMode="Tile" Viewport="0,0,40,40" ViewportUnits="Absolute">
           <DrawingBrush.Drawing>
             <GeometryDrawing Geometry="M0,0 L40,0 M0,0 L0,40">
               <GeometryDrawing.Pen><Pen Thickness="1" Brush="#1F374151"/></GeometryDrawing.Pen>
             </GeometryDrawing>
           </DrawingBrush.Drawing>
         </DrawingBrush>
         ```
         应用为内容 Grid 的 `Background="{StaticResource PageGridBrush}"`（外层 ScrollViewer Background=PageBackground）；
       - `PositionBadgeStyle`(Border, CornerRadius 2, Padding "10,2", Background `#190EA5E9`, BorderBrush `#4D0EA5E9`) 与红色变体 `PositionBadgeAlertStyle`(Background `#19EF4444`, BorderBrush `#66EF4444`)；
       - `StatusOkStyle`(Background `#1A22C55E`, BorderBrush `#4D22C55E`, 文字 Success, FontSize 10, Bold, Padding "8,3")；
       - `StatusAlertStyle`（红色变体，同上色系 `#26EF4444` / `#66EF4444`）；
       - `ResetAlarmButtonStyle`：Button 模板，Border CornerRadius 4 BorderThickness 2 BorderBrush=Primary Background=Surface，Foreground=Primary，Padding "24,10"；IsMouseOver 触发器改 Success + 绿色 DropShadow。
  3. 三张卡片放入 `UniformGrid Columns="3"`（每卡 `Margin="0,0,24,0"`，末卡 `Margin="0"`）。卡片结构（以 P-01 为模板，P-02/P-03 按参数表替换）：
     - 外层 `Border CornerRadius="4" BorderThickness="1" BorderBrush="{StaticResource Border}" Background="{StaticResource Surface}" Padding="20"`；
     - 卡内 `Grid`：`Grid.RowDefinitions` = Auto/Auto/Auto/Auto（表头/仪表/搭接示意/脚注）；
     - 角标装饰：4 个 16x16 Path（Primary 描边 2px）置于四角，Margin 6：
       TL `M 16 2 L 2 2 L 2 16`；TR `M 2 2 L 16 2 L 16 16`；BL `M 2 2 L 2 16 L 16 16`；BR `M 16 2 L 16 16 L 2 16`（Path Stretch=None，HorizontalAlignment/VerticalAlignment + Margin 组合定位四角；P-02 卡角标用 Error 色）；
     - 表头（行0，`Border BorderThickness="0,0,0,1" BorderBrush="{StaticResource Line}" Padding="0,0,0,12" Margin="0,0,0,16"` 内 DockPanel）：
       左：`Border Style=PositionBadgeStyle` 内 `TextBlock P-01`(FontSize 13 Bold, Consolas, Primary) + `TextBlock 前段搭接 / FRONT`(FontSize 12, Ink2, Margin 10,0,0,0)；
       右（Dock=Right）：P-01/P-03 为 `Border Style=StatusOkStyle` 文本 `NORMAL`；P-02 为横向 StackPanel：`Ellipse Width=10 Height=10 Fill=Error` + DropShadowEffect(红, BlurRadius 10, ShadowDepth 0) + `Border Style=StatusAlertStyle` 文本 `ALARM`；
     - 仪表（行1）：`Canvas Width="240" Height="180" HorizontalAlignment="Center"`，元素按下列坐标表绘制；
       读数叠放：Canvas 之下加 `StackPanel HorizontalAlignment="Center" Margin="0,-52,0,0"`：
       `TextBlock` 值(FontSize 38 Bold, Consolas, P-01/P-03=Success 且绿色 DropShadowEffect(BlurRadius 20, ShadowDepth 0, Opacity 0.4)；P-02=Error 且红色同名阴影) + `TextBlock "mm | 搭接量"`(FontSize 12, Ink3, Margin 0,4,0,0)；
     - 搭接示意（行2）：标签行 `DockPanel`：`TextBlock "搭接示意 / OVERLAP SCHEMATIC"`(FontSize 10, Ink3) + `Rectangle Height=1 Fill="#80374151"` 填充剩余；图区 `Viewbox Stretch="Uniform" MaxWidth="347"` 包 `Canvas Width="400" Height="110"`（坐标表见下）；
     - 脚注（行3，`Border BorderThickness="0,1,0,0" BorderBrush="{StaticResource Line}" Padding="0,12,0,0" Margin="0,16,0,0"` 内 Grid 左右两 TextBlock FontSize 10）：
       左 `标准: 4±2mm`（"标准: " Ink3 + "4±2mm" Ink2 Bold）；右 偏差：P-01 `+0.2`(Success Bold)、P-02 `+3.3 超限`(Error Bold)、P-03 `-0.2`(Success Bold)。
  4. 仪表 Canvas 坐标表（三卡通用部分）：
     - 轨道：`Path Data="M 45 140 A 75 75 0 1 1 195 140"` Stroke=Surface2 Thickness 12 StartLineCap/EndLineCap=Round；
     - 色带（通用）：红 `M 45 140 A 75 75 0 0 1 59 96`(Stroke=Error, Thickness 5, Opacity 0.45)；绿 `M 59 96 A 75 75 0 0 1 143 69`(Stroke=Success, Thickness 7, Opacity 0.5)；红 `M 143 69 A 75 75 0 0 1 195 140`(同红)；
     - 刻度（通用，Stroke=Ink3, Thickness 1.5）：`M 45 140 L 37 140`、`M 59 96 L 53 91`、`M 97 69 L 94 61`、`M 143 69 L 146 61`、`M 181 96 L 187 91`、`M 195 140 L 203 140`；
     - 数字（通用，TextBlock FontSize 9 Consolas, Ink3，Canvas.Left=x-5, Canvas.Top=y-9，text-anchor=middle 折算）：0@(28,153)、2@(46,86)、4@(92,53)、6@(148,53)、8@(194,86)、10@(212,153)；
     - 数值弧（已算好，直接抄）：
       P-01 `M 45 140 A 75 75 0 0 1 101.35 47.36`(Stroke=Success, Thickness 12, Round, 绿色 DropShadow BlurRadius 6 Opacity 0.5 ShadowDepth 0)
       P-02 `M 45 140 A 75 75 0 0 1 169.6 63.74`(Stroke=Error, 同阴影红色)
       P-03 `M 45 140 A 75 75 0 0 1 92.4 50.27`(Stroke=Success, 同阴影绿色)
     - 指针组：子 `Canvas` 设 `RenderTransform`：`RotateTransform Angle=X CenterX="120" CenterY="120"`，角度 P-01=-14.4 / P-02=41.4 / P-03=-21.6；组内：`Path Data="M 120 120 L 120 58"`(Stroke 同数值弧色, Thickness 3, Round, DropShadow 同色 BlurRadius 6 ShadowDepth 0 Opacity 0.6) + `Ellipse`(Left 112, Top 112, 16x16, Fill `#374151`, Stroke=Ink3, Thickness 2) + `Ellipse`(Left 117, Top 117, 6x6, Fill 同数值弧色)。
  5. 搭接示意 Canvas 坐标表（Canvas 400x110；矩形 y=45, h=40；尺寸线 y=38, 端刻线 y=34..42, 文字 Top≈20）：
     - P-01（绿）：A 左 21 / B 左 179（均 200x40）；重叠区 Left=179 Top=45 42x40（`Rectangle Stroke=Success StrokeDashArray="3 2" Fill="#1422C55E"`）；尺寸线 x 179→221(Stroke=Success) + 两端竖刻线(179/221, y34→42)；文字 `4.2mm`(Success, FontSize 10, Consolas, Canvas.Left=169, Width=62, TextAlignment=Center, Top=20)；
     - P-02（红）：A 左 37 / B 左 163；重叠区 Left=163 73x40（Stroke=Error, Fill `#1AEF4444`）；尺寸线 x 163→236 + 刻线(163/236)；文字 `7.3mm`(Error, Left=155, Width=88)；
     - P-03（绿）：A 左 19 / B 左 181；重叠区 Left=181 38x40（绿系）；尺寸线 x 181→219 + 刻线(181/219)；文字 `3.8mm`(Success, Left=176, Width=48)；
     - 材料矩形填充：A `LinearGradientBrush(0,0→1,1)` `#475569→#374151`，Border #6B7280；B `#5A6478→#475569`，Border #7B8290；B 在 A 之后绘制（自然置顶）；矩形内左上 `TextBlock 材料A/材料B`(FontSize 9, Foreground `#99FFFFFF`, Margin 由 Padding 模拟：Canvas.Left+8, Canvas.Top+4)。
  6. 报警横幅（标题行与卡片行之间）：`Border CornerRadius="4" BorderThickness="2" BorderBrush="{StaticResource Error}" Padding="24,14" Margin="0,16,0,20"`：
     - `Border.Effect` = DropShadowEffect(Color `#EF4444`, BlurRadius 24, ShadowDepth 0, Opacity 0.35)；
     - `Border.Triggers` EventTrigger RoutedEvent="Border.Loaded" → BeginStoryboard Storyboard(RepeatBehavior Forever, AutoReverse)：
       `ColorAnimation TargetProperty="(Border.BorderBrush).(SolidColorBrush.Color)" From="#EF4444" To="#66EF4444" Duration="0:0:0.8"`；
       `DoubleAnimation TargetProperty="(Border.Effect).(DropShadowEffect.Opacity)" From="0.7" To="0.3" Duration="0:0:0.8"`；
     - 内容 Grid：居中横向 StackPanel：`Ellipse 14x14 Fill=Error`(红 DropShadow BlurRadius 12 ShadowDepth 0) + `TextBlock "报警 | ALARM: P-02 中段搭接量超出标准范围 (4±2mm)"`(Error, FontSize 14, Bold, Margin 16,0,0,0) + 右侧同款 Ellipse(Margin 16,0,0,0)；注意 BorderBrush 需写成内联 `<Border.BorderBrush><SolidColorBrush Color="#EF4444"/></Border.BorderBrush>` 以便 ColorAnimation。
  7. P-02 报警卡片：外层 Border 用内联 SolidColorBrush(Color `#EF4444`) 作 BorderBrush + 同款 Loaded 触发器 Storyboard（ColorAnimation `#EF4444`↔`#66EF4444`、Effect.Opacity 0.7↔0.3、Effect.BlurRadius 50↔20，均 0.8s Forever AutoReverse）；Effect=DropShadowEffect(红, ShadowDepth 0)；角标描边改 Error。
  8. 重置按钮行：`Border HorizontalAlignment="Center" Margin="0,24,0,0"` 包 `Button Style=ResetAlarmButtonStyle`，内容 `StackPanel Orientation="Horizontal"`：`Path Data="M 3 12 A 9 9 0 1 0 6 5.3 M 3 4 L 3 9 L 8 9"`(Stretch=Uniform, Width 14, Height 14, Stroke=Primary, Thickness 3, Round) + `TextBlock "重置报警 / RESET ALARM"`(Margin 10,0,0,0)。无 Command。
  9. 页脚信息条：`Border CornerRadius="4" BorderBrush="{StaticResource Border}" BorderThickness="1" Background="{StaticResource Surface}" Padding="24,12" Margin="0,24,0,0"` 内居中 `TextBlock "SYS-ID: MLO-2026-A | 采样频率: 10Hz | 标准范围: 4±2mm | 传感器: 激光测距 x3"`(FontSize 11, Ink3)。

  **Must NOT do**: 禁止任何 `{Binding}`（除导航命令）；禁止 x:Name 以外的事件处理器；禁止修改 App.xaml 之外的全局样式；不写 C# 动画代码。

  **Recommended Agent Profile**: `deep`（长 XAML、几何参数多，需一次写对）。Skills: [`dotnet-wpf`]。

  **Parallelization**: NO；Blocks: 3、4；Blocked By: 1（App.xaml 模板引用本 View，必须同批存在才能编译）。

  **References**:
  - `D:\material_overlap_monitor.html:315-338, 375-398, 432-455` — 仪表 SVG 原始坐标（本计划已折算为 WPF Path Data，直接用计划值，不必再读 HTML）
  - `D:\material_overlap_monitor.html:344-358, 404-418, 461-475` — 搭接示意图与脚注参数
  - `D:\material_overlap_monitor.html:295-299, 480-495` — 横幅文案与页脚文字
  - `CX102PrickHMI/Views/MonitorView.xaml` — 现有页面排版风格（标题、Margin 24 外边距、PanelStyle 用法）
  - `CX102PrickHMI/MainWindow.xaml:82-115` — 现有 Ellipse.Style + DataTrigger 写法参考（本页不需要，仅风格参考）
  - WHY: 所有几何数值已在计划中折算完毕，执行者按表绘制即可，禁止自行重新推导。

  **Acceptance Criteria**:
  - [ ] `dotnet build "CX102PrickHMI.sln" --no-restore` → Build succeeded, 0 Error, 0 Warning
  - [ ] exe 启动 ≥7s 无未处理异常（stderr 为空）
  - [ ] 页面 XAML 中 `Binding` 仅出现在 MainWindow（导航命令），OverlapMonitorView.xaml 中 `{Binding` 出现次数为 0

  **QA Scenarios (MANDATORY)**:
  ```
  Scenario: 页面编译与启动
    Tool: Bash (PowerShell)
    Steps:
      1. dotnet build "CX102PrickHMI.sln" --no-restore → 期望 Build succeeded
      2. Start-Process bin\Debug\CX102PrickHMI.exe，重定向 stdout/stderr，等待 7s
      3. 检查进程 HasExited=False 且 stderr 无 "Exception"
    Expected Result: 构建成功；进程存活；无 XamlParseException
    Failure Indicators: CS/MC 错误；Unhandled Exception
    Evidence: .omo/evidence/task-2-build-run.txt

  Scenario: 零绑定静态校验
    Tool: Bash (PowerShell)
    Steps:
      1. (Select-String -Path "CX102PrickHMI\Views\OverlapMonitorView.xaml" -Pattern "\{Binding").Count
    Expected Result: 0
    Failure Indicators: >0
    Evidence: .omo/evidence/task-2-no-binding.txt

  Scenario: 数值保真校验
    Tool: Bash (PowerShell)
    Steps:
      1. Select-String -Path "CX102PrickHMI\Views\OverlapMonitorView.xaml" -Pattern "4\.2|7\.3|3\.8|4±2mm|\+3\.3 超限"
    Expected Result: 三卡片数值与偏差全部命中
    Failure Indicators: 任一缺失
    Evidence: .omo/evidence/task-2-values.txt
  ```

  **Commit**: NO（与 1、3 合并提交）

- [x] 3. 工程文件接线（csproj）

  **What to do**: 在 `CX102PrickHMI/CX102PrickHMI.csproj` 按现有条目风格追加：
  ```xml
  <Compile Include="ViewModels\OverlapMonitorViewModel.cs" />
  <Page Include="Views\OverlapMonitorView.xaml">
    <Generator>MSBuild:Compile</Generator>
    <SubType>Designer</SubType>
  </Page>
  <Compile Include="Views\OverlapMonitorView.xaml.cs">
    <DependentUpon>OverlapMonitorView.xaml</DependentUpon>
    <SubType>Code</SubType>
  </Compile>
  ```
  位置：Compile 放在 `ViewModels\HistoryAlarmViewModel.cs` 条目之后；Page 放在 `Views\HistoryAlarmView.xaml` 条目之后。

  **Must NOT do**: 不用通配符；不动既有条目；不添加 Fody/资源外内容。

  **Recommended Agent Profile**: `quick`。Skills: [`dotnet-wpf`]。

  **Parallelization**: NO；Blocks: 4；Blocked By: 2。

  **References**:
  - `CX102PrickHMI/CX102PrickHMI.csproj:94-103, 135-141` — 现有 ViewModel Compile 条目与 Views Page/Compile 条目格式
  - WHY: 旧式 csproj 不含通配符，漏加条目会直接编译失败。

  **Acceptance Criteria**:
  - [ ] 构建成功且 `Select-String -Path "CX102PrickHMI\CX102PrickHMI.csproj" -Pattern "OverlapMonitor"` 命中 3 处

  **QA Scenarios (MANDATORY)**:
  ```
  Scenario: csproj 完整性
    Tool: Bash (PowerShell)
    Steps:
      1. Select-String -Path "CX102PrickHMI\CX102PrickHMI.csproj" -Pattern "OverlapMonitor"
      2. dotnet build "CX102PrickHMI.sln" --no-restore
    Expected Result: ViewModel Compile + View Page + View Compile 共 3 处；构建成功
    Failure Indicators: 缺条目导致 CS0103/MC3074
    Evidence: .omo/evidence/task-3-csproj.txt
  ```

  **Commit**: YES（与 1、2 合并为一次提交）
  - Message: `feat(ui): add static overlap monitor page with nav switch`
  - Files: 上述全部新增/修改文件
  - Pre-commit: `dotnet build "CX102PrickHMI.sln" --no-restore`

- [x] 4. 构建与运行验证

  **What to do**:
  1. `dotnet build "CX102PrickHMI.sln" --no-restore` → 记录 0 Error/0 Warning；
  2. 启动 `CX102PrickHMI\bin\Debug\CX102PrickHMI.exe`（重定向输出/错误），存活 ≥7s 后关闭；
  3. 确认 stderr 无 Unhandled Exception / XamlParseException；
  4. （如有 UI 自动化条件）依次触发 ShowMonitorCommand / ShowOverlapCommand / ShowAlarmsCommand / ShowHistoryCommand 验证四页切换；否则记录为人工待验项。

  **Must NOT do**: 不为通过验证而放宽警告；不注释掉代码规避错误。

  **Recommended Agent Profile**: `quick`。Skills: [`dotnet-wpf`]。

  **Parallelization**: NO；Blocks: F1-F4；Blocked By: 1、2、3。

  **References**:
  - 本会话既有验证命令模式（PowerShell Start-Process + RedirectStandardError + HasExited 检查）
  - WHY: 该项目验证手段 = 构建 + 实际进程存活 + 错误流捕获。

  **Acceptance Criteria**:
  - [ ] 构建 0 Error / 0 Warning
  - [ ] 进程存活 7s，stderr 干净
  - [ ] 证据文件落盘 `.omo/evidence/task-4-final-run.txt`

  **QA Scenarios (MANDATORY)**:
  ```
  Scenario: 最终运行
    Tool: Bash (PowerShell)
    Steps:
      1. dotnet build "CX102PrickHMI.sln" --no-restore
      2. Start-Process exe (redirect) → sleep 7 → HasExited/ExitCode
      3. 输出全部落盘 evidence
    Expected Result: HasExited=False；stderr 无异常
    Failure Indicators: 退出/异常输出
    Evidence: .omo/evidence/task-4-final-run.txt
  ```

  **Commit**: NO

---

## Final Verification Wave

- [x] F1. **计划合规审计** — `oracle`：对照 Must Have / Must NOT Have 逐条核对文件与运行证据；检查无绑定、无 C# 业务逻辑、无服务接入。输出 `Must Have [N/N] | Must NOT Have [N/N] | VERDICT`。
- [x] F2. **代码质量审查** — `unspecified-high`：构建 0 Error/0 Warning；XAML 无绑定错误残留；样式键命名一致；无死代码。输出结论。
- [x] F3. **实际运行 QA** — `unspecified-high`：启动 exe ≥7s，捕获 stdout/stderr，确认无 XamlParseException；报告导航切换四页均正常。证据存 `.omo/evidence/`。
- [x] F4. **范围保真检查** — `deep`：diff 核对仅触及计划内文件；确认未修改现有三页面/ViewModel 业务；确认所有数值与 HTML 一致（4.2/7.3/3.8、42/73/38、偏差 +0.2/+3.3/-0.2）。输出 VERDICT。

## Commit Strategy
- Task 1-4 完成并验证后一次提交：`feat(ui): add static overlap monitor page with nav switch`

## Success Criteria
- 构建干净；四页导航正常；搭接量监控页视觉与 HTML 布局一致、配色为本地主题；无绑定/无业务逻辑。

