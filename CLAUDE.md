# CLAUDE.md - CX102PrickHMI 指南

## 概述
CX102 刺辊设备的上位机 HMI（WPF 桌面程序）：监控页显示配方/实际值、回零与使能状态、主机运行与维修模式，并对 PLC 下发点动命令和原点值；搭接监控页显示搭接测厚仪数据与超限报警；另有报警列表与历史报警页。数据通过 OPC UA 读写 PLC，报警记录用 SQLite 存储。

## 技术栈
- **框架**: .NET Framework 4.7.2 / WPF（旧式 csproj + packages.config，SDK 用 .NET 10.0.101 的 MSBuild 构建，global.json 已锁定）
- **UI 模式**: MVVM Pattern (使用 CommunityToolkit.Mvvm)，包括数据的属性通知，命令绑定，界面切换等等。
- **PLC 通信**: OPC UA（Opc.Ua.* + thinger.CommunicationLib 封装，`WriteNodeAsync<T>` 读写字典 `CurrentValue[VarName]`）
- **数据访问**: SqlSugar + SQLite（报警/历史记录存储）
- **UI 控件**: HslControls（`libs/HslControls.dll`，仪表盘等）；日志用 NLog；Fody/Costura 内嵌依赖 dll
- **UI 语言**: XAML
- **逻辑语言**: C#

## 代码检索
- 本项目已建 codegraph 索引（`.codegraph/`），符号级检索、调用关系、改动影响面优先用 codegraph MCP 查询；索引过期时在仓库根目录重跑 `codegraph init` 重建。

## 项目结构
代码都在 `CX102PrickHMI/` 项目文件夹内（仓库根目录只有解决方案、openspec、docs 和设计原型）：
- `CX102PrickHMI/Models/`: 定义数据模型和业务实体。
- `CX102PrickHMI/ViewModels/`: 包含视图的逻辑和命令处理，实现数据绑定。`MainViewModel` 是核心，承载 OPC 轮询与 500ms 快照泵。
- `CX102PrickHMI/Views/`: 包含 XAML 用户控件的定义（含 `NumericKeypadOverlay` 数字键盘）。
- `CX102PrickHMI/Services/`: 包含业务逻辑和数据存储服务（报警服务、SqlSugarHelper）。
- `CX102PrickHMI/Utilities/`、`Behaviors/`、`Interfaces/`、`Configure/`: 通用辅助类、附加行为（如 MomentaryButtonBehavior）、仓储接口、配置服务。
- `CX102PrickHMI/styles/`: 包含所有样式资源，其中有一个总的 Generic 资源，其他的样式资源都放在 Generic 里面（Colors.xaml、Controls.xaml），App.xaml 统一调用 Generic 资源。
- **PLC 点位表**: `Settings/settings.json` 是运行时文件，部署在 exe 同级目录（如 `bin/Debug/Settings/`），启动时由 MainViewModel 读取；不在源码树中维护。
- 根目录的 `spike-roller-hmi/` 与 `刺辊监控.html` 等是设计原型，仅作 UI 参考，不参与构建。

## 常用命令
- **构建项目**: `dotnet build`（在仓库根目录构建解决方案）
- **运行应用**: 构建 `dotnet run` 不适用于本项目（旧式 csproj），请直接运行 `CX102PrickHMI/CX102PrickHMI/bin/Debug/CX102PrickHMI.exe`

## 编码规范
- **MVVM 强制执行**: 严格遵循 MVVM 模式，禁止在 `.xaml.cs` (Code-behind) 文件中编写业务逻辑。所有 UI 交互逻辑应通过 `RelayCommand` 和 `ObservableProperty` 等 MVVM 工具实现。
- **命名约定**: 属性和方法使用 PascalCase，私有字段使用 camelCase 并带有 `_` 前缀。
- **XAML 最佳实践**: 
  - 优先使用 `StaticResource` 或 `DynamicResource` 来定义和应用样式、模板和转换器。
  - 布局管理优先使用 `Grid` 和 `StackPanel`。
  - 使用 `d:DataContext` (设计时数据上下文) 来在 Visual Studio 设计器中预览数据绑定效果。
- **异步编程**: 对于 I/O 操作或耗时任务，始终使用 `Async/Await` 模式，以确保 UI 的响应性。


## 调试指南
- **绑定问题**: 当遇到 XAML 数据绑定失效时，请检查 Visual Studio 的 **Output (输出)** 窗口，其中会显示详细的绑定错误信息。将这些错误信息提供给 AI 助手进行分析。
- **DataContext 验证**: 确保 XAML 视图的 `DataContext` 已正确设置，无论是通过代码还是 XAML 标记。

## 工作流规则
- **原子性提交**: 每次提交应只包含一个逻辑上的变更。
- **功能分支**: 在进行任何新功能开发或 Bug 修复前，请先创建新的功能分支。
- **测试驱动**: 鼓励为新功能编写单元测试，并确保现有测试通过。
