# Proposal

## Why

报警链路目前是"断"的：PLC 已经通过 OPC UA 轮询触发 `AlarmTriggerEvent`（事件值 true/false 表示到达/离开），但 `MainViewModel.PlcDevice_AlarmTriggerEvent` 的处理体是注释掉的桩代码；`AlarmService` 只有 `Insert` 没有查询；`HistoryAlarmView` 的日期、级别、导出、分页全是写死的原型占位。同时各视图引用样式资源的方式没有统一约束（App.xaml → `styles/Generic.xaml` → Colors/Controls 合并链），旧式 csproj 要求新资源字典显式注册 `Page` 条目，否则运行时静默找不到资源。需要在更多页面叠加之前把报警生命周期、持久化、查询打通，并把样式资源的使用方式固化下来。

## What Changes

- **报警事件落库**：`AlarmTriggerEvent` 中，事件值为 `true` 记为 `到达`，为 `false` 记为 `离开`；每个事件都通过 `AlarmService`（SqlSugar/SQLite）写入一条 `Alarms` 记录，填充 `InsertTime`、`AlarmState`（到达/离开）、`AlarmNote`、`VarName`，其余可空字段（`Symbol`）留空。
- **术语映射**：运行时 `Settings/settings.json` 的 OPC UA Alarm 组（48 个变量，其中高限报警使能 10 点、低限报警使能 3 点）使用字段 `HighAlarmEnable/HighAlarmNote`（高限报警使能）与 `LowAlarmEnable/LowAlarmNote`（低限报警使能）。用户口径的 `IsMaxAlarm`/`IsMinAlarm` 即分别对应这两个使能字段，本文档及后续产物沿用此等价映射，不改运行时文件结构。
- **实时报警列表（AlarmListView）**：收到 `到达` 时向 `AlarmItems` 加入一行（报警时间、报警内容、状态），激活中的报警行状态 SHALL 显示 `到达`，收到 `离开` 时移除对应行；`ItemsControl` 绑定改为生效。
- **历史报警查询重构（HistoryAlarmView）**：按日期范围查询 `Alarms` 记录，展示报警时间、报警内容、恢复时间（由同一变量的 `离开` 记录配对得到）；范围校验要求起止合法且跨度不超过两天（48 小时），超限给出提示。
- **HistoryAlarmView 移除项**：删除级别列、级别筛选 ComboBox（全部级别/错误/警告/信息）、"导出"按钮与固定分页文本（"第 1 / 1 页"占位）；日期输入框改为真实数据绑定。
- **样式资源约定**：视图统一通过 App.xaml 合并的 `styles/Generic.xaml`（其内合并 `Colors.xaml`、`Controls.xaml`）使用 `StaticResource` 样式；如新增资源字典，必须在旧式 csproj 中显式添加 `<Page Include>` 条目。

## Capabilities

### New Capabilities

- `alarm-history`: PLC 报警事件的生命周期能力——到达/离开事件捕获与落库、实时报警列表的加入/移除维护（激活行状态显示 `到达`）、历史报警的日期范围查询（含跨度两天上限校验）与报警时间/内容/恢复时间三列展示。
- `style-resources`: 样式资源的组织与使用约定——App.xaml 合并 Generic，Generic 合并 Colors/Controls 的单一入口，视图经 StaticResource 消费，以及新资源字典的 csproj Page 注册要求。

### Modified Capabilities

<!-- 现有规格库存仅 overlap-splice-monitor（搭接超限监控），与本次报警生命周期/样式约定无关，不做修改。 -->

## Impact

- **代码**：`ViewModels/MainViewModel.cs`（报警事件处理、`AlarmItems`/`HistoryItems`）、`Services/AlarmService.cs`（新增查询方法）、`Interfaces/IAlarmRepository.cs`、`Models/Alarms.cs`、`Views/AlarmListView.xaml`（级别列改为状态列，显示 `到达`）、`Views/HistoryAlarmView.xaml`（移除级别列/级别筛选 ComboBox/导出按钮/分页文本，补真实绑定）、`styles/`（如有新字典则同步 `CX102PrickHMI.csproj` 的 Page 条目）。
- **数据**：SQLite `Alarms` 表（SqlSugar 实体现有可空列已覆盖所需字段，预计无需改表结构）。
- **配置**：运行时 `CX102PrickHMI/bin/Debug/Settings/settings.json` 只读消费，`IsMaxAlarm`→`HighAlarmEnable`、`IsMinAlarm`→`LowAlarmEnable` 仅作概念映射。
- **依赖**：无新增依赖。
