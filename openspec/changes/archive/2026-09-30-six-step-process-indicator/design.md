# Design

## Context

- 单 MainViewModel + 500ms 快照泵 + ValueLock 互斥已就位;`Steps` 为空 `ObservableCollection<ProcessStep>`,MainWindow 顶部步骤栏 (ItemsControl + UniformGrid + ProcessStep DataTemplate) 绑定 `Steps` 但无数据项,故界面空白——非遮挡问题。
- `ProcessStep` 模型含 `Number/Title/Status/State` 与 `UpdateState(ProcessStepState, string)`,DataTemplate 已有 Completed(绿)/Pending(灰)/Error(红) 触发器。
- MonitorView 设备图片边框已有红色脉冲动画 (ColorAnimation + DoubleAnimation, AutoReverse Forever),由 `IsAlarmActive` DataTrigger 驱动;`IsAlarmActive` 上一轮被恒置 false。
- settings.json 已含全部 19 个点位 (6 步骤 + 13 报警),无需改 JSON。

## Decisions

- **D1 步骤播种**:构造函数直接 new 6 个 `ProcessStep` (标题按用户命名,初始 Pending/"未开始"),替换空集合初始化;不引入新模型类。
- **D2 泵刷新**:泵内按索引调用 `Steps[i].UpdateState(点位true ? Completed : Pending, 点位true ? "已完成" : "未开始")`;步骤 1 用 `MainMachineStop` 原值 (true→Completed,语义即"停机达成")。UpdateState 内部应自行判重避免重复 PropertyChanged (ProcessStep 现有实现已如此)。
- **D3 报警源**:泵内定义 `private static readonly string[] AlarmTagNames = { 13 个点位 }`,每周期在 ValueLock 内逐个 TryGetValue+ParseBool 取 OR,结果写入既有 `IsAlarmActive` (改为带 backing field 的 SetProperty,替换恒 false 的 get-only 实现)。MonitorView 既有 DataTrigger + 脉冲动画无需改动。
- **D4 取消红色**:任何步骤不调用 `UpdateState(Error, …)`;`ProcessStepState.Error` 枚举保留不删 (避免动模型与 XAML 触发器)。
- **D5 范围**:不改 MainWindow.xaml / MonitorView.xaml / settings.json;仅 MainViewModel.cs。

## Risks

- 13 个报警点位逐个锁内读取:每周期 19 次 lock+TryGetValue,微秒级,可忽略。
- `ProcessStep.UpdateState` 若在相同状态下重复调用会产生冗余 PropertyChanged——需确认/保证其内部有状态判重 (实现时检查模型代码,若无则在调用侧判重)。
