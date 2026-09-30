# process-step-indicator Specification

## Purpose
TBD - created by archiving change six-step-process-indicator. Update Purpose after archive.

## Requirements

### Requirement: 六步流程指示实点绑定
流程步骤栏 SHALL 显示 6 个步骤,顺序与点位固定为:1 主线停机←`MainMachineStop`、2 配方加载←`StsStepRcpLoaded`、3 槽辊打开←`StsStepRollerOpen`、4 位置到达←`StsStepAxisInPos`、5 传送带回退←`StsStepConveyorDone`、6 槽辊收回←`StsStepRollerClosed`。步骤状态由 500ms 快照泵从 `CurrentValue[VarName]` 刷新。

#### Scenario: 点位翻转驱动步骤状态
- **WHEN** `StsStepRollerOpen` 由 false 变为 true
- **THEN** 步骤 3 "槽辊打开" 在下一个泵周期内变为绿色"已完成"
- **WHEN** `StsStepRollerOpen` 变回 false
- **THEN** 步骤 3 变回灰色"未开始"

### Requirement: 步骤状态二态规则 (取消红色)
每个步骤 SHALL 仅有两种状态:点位 true → 绿色"已完成";点位 false → 灰色"未开始"。红色"异常"态 SHALL 不再使用 (任何步骤不得进入 Error 态)。步骤 1 (主线停机) 同样适用该规则:`MainMachineStop=true` → 绿色"已完成" (停机),false → 灰色"未开始"。

#### Scenario: 主线停机极性
- **WHEN** `MainMachineStop = true`
- **THEN** 步骤 1 "主线停机" 显示绿色"已完成"
- **WHEN** `MainMachineStop = false`
- **THEN** 步骤 1 显示灰色"未开始"

#### Scenario: 无红色态
- **WHEN** 任意点位组合变化
- **THEN** 没有任何步骤进入红色"异常"状态

### Requirement: 设备图片报警呼吸闪烁
系统 SHALL 以 13 个 PLC 报警点位作为报警源:高报警 `AxisInCommError`、`AxisInError`、`AxisInOverload`、`AxisOutCommError`、`AxisOutError`、`AxisOutOverload`、`ConveyorBackTimeout`、`GroovedExtTimeout`、`GroovedRetTimeout`、`HeartbeatLost` (true=报警),低报警 `LimitBetween`、`LimitSwitchIn`、`LimitSwitchOut` (true=报警)。任一为真 → 监控页设备图片边框执行红色呼吸闪烁 (复用现有脉冲动画);全部为假 → 停止闪烁并还原边框。搭接量超限 (SpliceValue) 不属于本报警源。

#### Scenario: 报警触发与恢复
- **WHEN** `AxisInError` 变为 true
- **THEN** 设备图片边框开始红色呼吸闪烁
- **WHEN** `AxisInError` 变回 false 且无其他报警点位为真
- **THEN** 边框停止闪烁并还原

#### Scenario: 多源报警
- **WHEN** `HeartbeatLost` 为 true 期间 `LimitSwitchIn` 也变为 true
- **THEN** 闪烁持续
- **WHEN** 两者都恢复 false
- **THEN** 闪烁停止
