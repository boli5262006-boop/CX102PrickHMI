# Splice Gauges / CmdReset / Keypad — Specs

## ADDED Requirements

### Requirement: 搭接仪表实点绑定
系统 SHALL 在搭接监控页将 3 个 HslArcGauge 的 Value 绑定到 `SpliceValue1/2/3` 的实时值 (500ms 快照,Double),量程 Min=0 Max=8,目标线 SettingValue=4。

#### Scenario: 实时值显示
- **WHEN** PLC 上报 SpliceValue2 = 4.5
- **THEN** 中段仪表读数显示 4.5 (F1 格式),示意图 mm 标签显示 `4.5mm`

### Requirement: 搭接超限判定与联动
超限 SHALL 判定为 `v < 2 || v > 6` (4±2mm 含边界内)。超限时该卡片:边框红色脉冲动画 (与原模拟效果一致)、徽章 NORMAL→ALARM、仪表弧色 Success→Error、Δ 文本追加 ` 超限` 且变红、四角饰条 Primary→Error;恢复正常时全部还原。

#### Scenario: 超限进入与恢复
- **WHEN** SpliceValue1 从 4.2 变为 7.3
- **THEN** P-01 卡片进入报警态 (脉冲、ALARM、Error 弧色、Δ 显示 `+3.3 超限`)
- **WHEN** SpliceValue1 恢复 3.9
- **THEN** P-01 卡片还原 NORMAL 态,Δ 显示 `-0.1`

### Requirement: 搭接报警横幅常驻双态
横幅 SHALL 常驻显示:全部正常 → 绿色 `检测正常 | SYSTEM NORMAL` (无动画);任一超限 → 红色脉冲 + 文本 `报警 | ALARM: <位号列表> 搭接量超出标准范围 (4±2mm)` (位号列表动态)。

#### Scenario: 横幅双态切换
- **WHEN** 仅 SpliceValue2 超限
- **THEN** 横幅显示 `报警 | ALARM: P-02 中段 搭接量超出标准范围 (4±2mm)` 并脉冲
- **WHEN** 全部回到范围内
- **THEN** 横幅显示 `检测正常 | SYSTEM NORMAL`,停止脉冲

### Requirement: CmdReset 脉冲命令
点击顶栏复位按钮 SHALL 写 `CmdReset`=true,50ms 后写 false (经 VarName→VarAddress 反查 + WriteNodeAsync\<bool\>,失败反馈 CalibrationMessage)。

#### Scenario: 复位脉冲
- **WHEN** 点击复位按钮且 PLC 在线
- **THEN** PLC 收到 CmdReset=true,约 50ms 后收到 false

### Requirement: 数字键盘首键替换
数字键盘打开时 SHALL 显示当前值并进入"首键替换"状态:首个数字整体替换缓冲 (先按 `.` 补为 `0.`),此后正常追加;C 清空后回到首键替换状态。

#### Scenario: 首键替换
- **WHEN** 键盘打开显示 0.00 后按下 `5`
- **THEN** 缓冲变为 `5` (而非 `0.005`)
- **WHEN** 随后按下 `.` 与 `2`
- **THEN** 缓冲变为 `5.2`

### Requirement: 设置按钮新配方闪烁指示
MonitorView 设置按钮背景 SHALL 跟随 `StsNewRcpBlink`:true → Primary (与按压色一致),false → Surface2;点动写 `CmdSetup` 行为不变。

#### Scenario: 新配方闪烁
- **WHEN** StsNewRcpBlink 以 1Hz 翻转
- **THEN** 设置按钮背景以约 1Hz 在 Primary 与 Surface2 间闪烁
