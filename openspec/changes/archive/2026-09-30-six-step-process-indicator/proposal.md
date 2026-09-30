# Proposal

## Why

监控页顶部的流程步骤栏自上一轮改造后一直空白 (Steps 集合按当时范围决定被清空,等待步骤→点位映射确认)。现已确认 6 步流程与 PLC 点位的对应关系,需要把步骤指示接到真实信号;同时设备图片的报警脉冲此前因无报警源而恒关,现在接入真实报警点位让"有报警就呼吸闪烁"生效。

## What Changes

- 流程步骤栏从空集合改为 6 个真实步骤,顺序与点位:
  1. 主线停机 ← `MainMachineStop` (true=停机)
  2. 配方加载 ← `StsStepRcpLoaded`
  3. 槽辊打开 ← `StsStepRollerOpen`
  4. 位置到达 ← `StsStepAxisInPos`
  5. 传送带回退 ← `StsStepConveyorDone`
  6. 槽辊收回 ← `StsStepRollerClosed`
- 步骤状态规则:点位 true → 绿色"已完成",false → 灰色"未开始";**取消红色"异常"指示** (Error 态不再使用)。
- 设备图片边框接真实报警源:13 个 PLC 报警点位 (高报警 10: `AxisInCommError/AxisInError/AxisInOverload/AxisOutCommError/AxisOutError/AxisOutOverload/ConveyorBackTimeout/GroovedExtTimeout/GroovedRetTimeout/HeartbeatLost`;低报警 3: `LimitBetween/LimitSwitchIn/LimitSwitchOut`,true=触发) 任一为真 → 边框红色呼吸闪烁 (复用现有动画);无报警时不闪。
- 500ms 快照泵新增上述 19 个点位的读取;不引入其他改动。

## Capabilities

### New Capabilities

- `process-step-indicator`: 流程步骤栏 6 步实点绑定与状态规则;设备图片报警呼吸闪烁的报警源判定。

### Modified Capabilities

（无——`monitor-plc-binding` 尚未归档基线化,本变更独立成新 capability。）

## Impact

- `ViewModels/MainViewModel.cs`:构造函数播种 6 个 ProcessStep;泵新增 6 个步骤点位 + 13 个报警点位的快照判定;`IsAlarmActive` 从恒 false 改为真实报警 OR。
- `MainWindow.xaml`:无改动 (步骤栏 DataTemplate 已支持 Completed/Pending 渲染)。
- `Views/MonitorView.xaml`:无改动 (脉冲 DataTrigger 已绑定 IsAlarmActive)。
- settings.json:无改动 (所有点位已定义)。
