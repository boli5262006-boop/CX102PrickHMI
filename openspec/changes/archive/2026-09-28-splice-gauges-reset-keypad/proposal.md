# Proposal

## Why

搭接监控页 (OverlapMonitorView) 目前整页是静态假数据 (横幅常驻假报警、仪表值 4.2/7.3/3.8 写死、NORMAL/ALARM 徽章固定)。settings.json 已提供 SpliceValue1/2/3 (Float) 实点,需要把该页接到真实数据;另有机型操作补齐:顶栏复位按钮 (CmdReset)、数字键盘"首键替换"输入体验、设置按钮的新配方闪烁指示 (StsNewRcpBlink)。

## What Changes

- 搭接页 3 个 HslArcGauge 绑定 `SpliceValue1/2/3` 实时值;超限判定 `v < 2 || v > 6` (4±2mm 含边界内)。
- 超限联动:卡片红色边框脉冲动画 (与原 P-02 模拟效果一致)、NORMAL↔ALARM 徽章、仪表弧色 Success↔Error、Δ 文本 (`+0.2` / `+3.3 超限`)、四角饰条颜色、示意图 mm 标签文本 (矩形示意保持静态)。
- 顶部横幅常驻:全部正常 → 绿色 "检测正常 | SYSTEM NORMAL";任一超限 → 红色脉冲 + 动态文本列出超限位号。
- 顶栏新增 复位 按钮 (HoldButtonStyle):点击写 `CmdReset`=true,50ms 后写 false。
- 设置按钮背景跟随 `StsNewRcpBlink`:true → Primary (与按压色一致),false → Surface2;点动写行为不变。
- 数字键盘:打开显示当前值并进入"首键替换"状态——首个数字/小数点整体替换缓冲 (先按 `.` 自动补 `0.`),之后正常追加;C 清空后回到首键替换状态。

## Capabilities

### New Capabilities

- `overlap-splice-monitor`: 搭接页仪表实点绑定、超限判定与卡片/横幅报警联动。

### Modified Capabilities

- `monitor-plc-binding`: 新增 ADDED 行为——CmdReset 脉冲命令、数字键盘首键替换输入、设置按钮 StsNewRcpBlink 背景指示。(原需求不变,见该 capability 现有 spec。)

## Impact

- `ViewModels/MainViewModel.cs`:泵新增 SpliceValue1/2/3 (double)、OutOfRange (bool)、Delta (string)、HasAnySpliceAlarm/BannerText、StsNewRcpBlink;新增 ResetCommand (true→50ms→false);键盘 fresh-input 标志。
- `Views/OverlapMonitorView.xaml`:整页假数据替换为绑定 + DataTrigger 联动。
- `Views/MonitorView.xaml`:设置按钮局部样式加 StsNewRcpBlink 触发。
- `MainWindow.xaml`:顶栏加复位按钮。
- settings.json 已由用户更新 (CmdReset=Bool ns=4;i=344;SpliceValue1/2/3=Float),HMI 侧无需再改。
