# overlap-splice-monitor Specification

## Purpose
TBD - created by archiving change splice-gauges-reset-keypad. Update Purpose after archive.

## Requirements

### Requirement: 搭接仪表实点绑定
系统 SHALL 在搭接监控页将 3 个 HslArcGauge 的 Value 绑定到 `SpliceValue1/2/3` 的实时值 (500ms 快照,Double),量程 Min=0 Max=8,目标线 SettingValue=4。

#### Scenario: 实时值显示
- **WHEN** PLC 上报 SpliceValue2 = 4.5
- **THEN** 中段仪表读数显示 4.5 (F1 格式),示意图 mm 标签显示 `4.5mm`

### Requirement: 搭接超限判定与联动
每通道 SHALL 按 500ms 快照值 v 判定三段状态,优先级 报警(红) > 预警(黄) > 正常(绿):
- **报警 (红)**:`v < 2 || v > 6` (超出 4±2mm;精确值 2.0 与 6.0 不判红)。
- **预警 (黄)**:`2 <= v < 3 || 5 < v <= 6` (超出 4±1mm 但仍在 4±2mm 内;精确值 3.0 与 5.0 不判黄)。
- **正常 (绿)**:`3 <= v <= 5`。

各段卡片联动 SHALL 如下:
- **正常**:边框常规色、无脉冲动画;徽章文本 `NORMAL` (Success 色);仪表弧色 Success;Δ 文本为纯偏差 (如 `-0.1`,Success 色);四角饰条与 P-xx 位置标签 Primary;示意图搭接高亮块、尺寸线与 mm 值文本 Success。
- **预警**:边框 Warning 色黄色脉冲动画 (呼吸节奏与红色报警一致,颜色为 Warning);徽章文本 `WARN` (Warning 色);仪表弧色 Warning;Δ 文本追加 ` 预警` 且变 Warning 色;四角饰条与位置标签 Warning;示意图搭接高亮块、尺寸线与 mm 值文本 Warning。
- **报警** (与原红色行为一致):边框红色脉冲动画;徽章文本 `ALARM` (Error 色);仪表弧色 Error;Δ 文本追加 ` 超限` 且变红;四角饰条与位置标签 Error;示意图搭接高亮块、尺寸线与 mm 值文本 Error。

状态 SHALL 按每个快照无锁存重算:恢复沿同一边界反向逐级进行 (红→黄→绿或绿→黄→红),全部联动随状态同步还原。仪表量程 (Min=0, Max=8)、目标线 4、F1 格式、Δ 基准 4.0 与 500ms 轮询节奏不变;该三段判定仅驱动 UI,PLC 报警点与数据库报警记录路径不受影响。

#### Scenario: 超限进入与恢复
- **WHEN** SpliceValue1 从 4.2 变为 7.3
- **THEN** P-01 卡片进入报警态 (红色脉冲、ALARM、Error 弧色、Δ 显示 `+3.3 超限`、示意图 Error 高亮)
- **WHEN** SpliceValue1 恢复 3.9
- **THEN** P-01 卡片还原 NORMAL 态,Δ 显示 `-0.1`

#### Scenario: 预警进入与恢复
- **WHEN** SpliceValue2 从 4.0 变为 5.8
- **THEN** P-02 卡片进入预警态 (黄色脉冲呼吸、WARN、Warning 弧色、Δ 显示 `+1.8 预警`、示意图 Warning 高亮)
- **WHEN** SpliceValue2 恢复 4.9
- **THEN** P-02 卡片还原 NORMAL 态,Δ 显示 `+0.9`

#### Scenario: 边界值归属
- **WHEN** SpliceValue1 = 2.0 (或 6.0)
- **THEN** P-01 为预警态 (黄色),不触发红色报警
- **WHEN** SpliceValue1 = 3.0 (或 5.0)
- **THEN** P-01 为正常态 (绿色),无预警

#### Scenario: 分段恢复无锁存
- **WHEN** SpliceValue1 从 7.3 降至 5.5
- **THEN** P-01 从报警态进入预警态 (WARN、Warning 色、脉冲切换为黄色呼吸)
- **WHEN** SpliceValue1 继续降至 4.5
- **THEN** P-01 还原 NORMAL 态

### Requirement: 搭接报警横幅常驻双态
横幅 SHALL 常驻显示,按每个 500ms 快照三段无锁存重算,红色优先于黄色:
- 全部通道正常 → 绿色 `检测正常 | SYSTEM NORMAL` (无动画)。
- 至少一个通道预警且无通道报警 → 黄色脉冲呼吸 (与红色报警脉冲节奏一致、颜色为 Warning),文本 `预警 | WARNING: <预警位号列表> 搭接量接近标准范围 (4±1mm)` (位号列表动态,可含多个通道)。
- 任一通道报警 → 红色脉冲 + 文本 `报警 | ALARM: <报警位号列表> 搭接量超出标准范围 (4±2mm)` (位号列表动态);此时即使仍有通道处于预警,横幅 SHALL 保持红色报警态 (红色优先),红色文本仅列出报警位号。

位号沿用 P-01 前段 / P-02 中段 / P-03 后段。恢复时横幅随各通道跨过 2/3、5/6 边界逐级切换 红→黄→绿,每个快照独立判定,不引入锁存或迟滞。

#### Scenario: 横幅双态切换
- **WHEN** 仅 SpliceValue2 报警 (如 7.3)
- **THEN** 横幅显示 `报警 | ALARM: P-02 中段 搭接量超出标准范围 (4±2mm)` 并红色脉冲
- **WHEN** 全部通道回到正常带内
- **THEN** 横幅显示 `检测正常 | SYSTEM NORMAL`,停止脉冲

#### Scenario: 预警态横幅
- **WHEN** 仅 SpliceValue1 预警 (如 5.8) 且无通道报警
- **THEN** 横幅显示黄色 `预警 | WARNING: P-01 前段 搭接量接近标准范围 (4±1mm)`,无红色脉冲
- **WHEN** SpliceValue1 恢复 4.5
- **THEN** 横幅恢复绿色 `检测正常 | SYSTEM NORMAL`

#### Scenario: 红色优先于黄色
- **WHEN** SpliceValue1 预警 (5.8) 且 SpliceValue3 报警 (1.5)
- **THEN** 横幅保持红色报警态并列出报警位号 P-03 后段,持续脉冲
- **WHEN** SpliceValue3 回到预警带 (如 2.5) 而 SpliceValue1 仍预警
- **THEN** 横幅切换为黄色预警态,列出 P-01 前段 与 P-03 后段,无红色脉冲

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
