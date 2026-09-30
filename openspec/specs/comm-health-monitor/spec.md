# comm-health-monitor Specification

## Purpose
TBD - created by archiving change jog-buttons-comm-watch. Update Purpose after archive.

## Requirements

### Requirement: 四点动按钮按轴分组
监控页 SHALL 提供四个按住式点动按钮,两行布局:第一行 点动前进内侧 (`CmdJogInFwd`)、点动后退内侧 (`CmdJogInBwd`)、点动前进外侧 (`CmdJogOutFwd`)、点动后退外侧 (`CmdJogOutBwd`);第二行 校准内侧、校准外侧、设置 (现有不变)。全部沿用按住式行为 (按下写 true、松开或失去捕获写 false),经 VarName→VarAddress 反查 + `WriteNodeAsync<bool>` 写入。旧点位 `CmdJogFwd/CmdJogBwd` 的绑定 SHALL 移除。

#### Scenario: 点动内侧前进
- **WHEN** 按住 "点动前进内侧"
- **THEN** PLC `CmdJogInFwd` 收到 true;松开后收到 false

#### Scenario: 旧点位不再被引用
- **WHEN** 检索 MainViewModel
- **THEN** 不存在对 `CmdJogFwd`/`CmdJogBwd` 的写入

### Requirement: HMIHeart 心跳冻结判定
系统 SHALL 以 `HMIHeart` (Int, ns=4;i=540) 心跳计数判断 HMI 与 PLC 通讯健康:沿用现有 PLCCOM 循环——心跳值较上一周期无变化计一次失败,连续失败达到 `MaxHeartbeatFailures`(4,约 4 秒) 判定**通讯异常**;心跳值恢复变化即判定**通讯恢复**。判定结果驱动 `IsCommAbnormal` 标志 (PropertyChanged 通知需在 UI 线程)。

#### Scenario: 心跳冻结判异常
- **WHEN** HMIHeart 值连续 4 个检查周期 (约 4 秒) 无变化
- **THEN** `IsCommAbnormal` 变为 true

#### Scenario: 恢复
- **WHEN** `IsCommAbnormal` 为 true 期间 HMIHeart 值发生变化
- **THEN** `IsCommAbnormal` 变回 false

### Requirement: 通讯异常 DialogWindow (不可消除)
`IsCommAbnormal` 变为 true 时 SHALL 弹出置顶 DialogWindow:无确定按钮 (不可手动消除)、红色边框呼吸闪烁、文案标题 "PLC 通讯异常"、副文 "通讯恢复后此提示自动消失";`IsCommAbnormal` 变回 false 时 SHALL 自动关闭。弹窗为非模态 (Show),不阻塞主界面;重复触发不得弹出第二个实例。

#### Scenario: 异常弹出与自动关闭
- **WHEN** 心跳冻结达 4 秒
- **THEN** 弹出通讯异常 DialogWindow (无确定按钮,边框红色呼吸)
- **WHEN** 通讯恢复
- **THEN** 弹窗自动关闭

#### Scenario: 不重复弹出
- **WHEN** 弹窗已显示且异常持续
- **THEN** 不弹出第二个实例

### Requirement: 历史报警查询失败弹框
历史报警查询发生异常时 SHALL 弹出同一 DialogWindow 组件 (**带确定按钮**,模态 ShowDialog),标题 "查询失败"、副文为错误摘要;点击确定关闭。

#### Scenario: 查询失败弹框
- **WHEN** 历史报警查询抛出异常
- **THEN** 弹出 DialogWindow 显示错误摘要,含确定按钮
- **WHEN** 点击确定
- **THEN** 弹窗关闭

### Requirement: 底栏 PLC连接状态指示
主窗口底栏 SHALL 显示静态文本 "PLC连接状态" 与相邻圆形指示:通讯正常 → 绿色;通讯异常 (`IsCommAbnormal=true`) → 红色。版本号文本保留于右侧。

#### Scenario: 底栏状态翻转
- **WHEN** `IsCommAbnormal` 从 false 变 true
- **THEN** 圆形由绿变红
- **WHEN** 恢复 false
- **THEN** 圆形变回绿色
