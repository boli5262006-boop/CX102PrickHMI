# Proposal

## Why

点动控制需要按轴 (内侧/外侧) 分别提供前进/后退四个点动按钮,旧的两个点动绑定点位 (`CmdJogFwd/CmdJogBwd`) 已由 PLC 侧作废;同时 HMI 与 PLC 的通讯健康目前完全不可见——心跳冻结 (连接中断/PLC 停机) 时操作员毫无感知,需要在底栏常驻状态指示 + 异常时弹出不可忽略的报警框。

## What Changes

- 监控页点动按钮改为 4 个 (两行布局):
  - 第一行:点动前进内侧 (`CmdJogInFwd`)、点动后退内侧 (`CmdJogInBwd`)、点动前进外侧 (`CmdJogOutFwd`)、点动后退外侧 (`CmdJogOutBwd`)
  - 第二行:校准内侧 (`CmdHomingIn`)、校准外侧 (`CmdHomingOut`)、设置 (`CmdSetup`) 不变
  - 旧点位 `CmdJogFwd/CmdJogBwd` 绑定作废删除;全部沿用按住式点动 (按下 true/松开 false)
- 新增可复用 `DialogWindow` 弹窗组件 (深色卡片 + 红色边框呼吸动画 + ⚠ 图标 + 标题/副文 + 可选确定按钮):
  - **通讯异常**:HMIHeart (Int, ns=4;i=540) 心跳值冻结超过 `MaxHeartbeatFailures`(4) 秒 → 弹出 (无确定按钮、不可手动消除、置顶),通讯恢复 (心跳值变化) 自动关闭
  - **历史报警查询失败**:查询异常时弹出同一组件 (带确定按钮,模态),文案为错误信息
- 底栏改造:`FooterStatus` 文案替换为静态 "PLC连接状态" + 相邻圆形指示 (通讯正常=绿、异常=红,随 `IsCommAbnormal` 翻转);Version 保留
- 心跳判定沿用现有 `ReadHeartbeat/PLCCOM` int 递增逻辑 (`HMIHeart` 已改 Int 类型) 与 `MaxHeartbeatFailures=4`;新增 `IsCommAbnormal` 标志位驱动弹框与圆点

## Capabilities

### New Capabilities

- `comm-health-monitor`: HMIHeart 心跳冻结判定、`IsCommAbnormal` 标志、通讯异常 DialogWindow 生命周期 (弹出/自动关闭)、底栏 PLC连接状态圆点。

### Modified Capabilities

（无——`process-step-indicator` 已归档基线化;点动按钮属监控页操作面板扩展,随本变更一并落地,不改变已有 capability 的需求。）

## Impact

- 新增 `Views/DialogWindow.xaml` + `.cs` (含 csproj Page/Compile 注册)
- `ViewModels/MainViewModel.cs`:4 个点动命令替换、`IsCommAbnormal` 标志、PLCCOM 内弹框生命周期管理 (UI 线程调度)、`FooterStatus` 属性移除
- `Views/MonitorView.xaml`:按钮区两行布局
- `MainWindow.xaml`:底栏加圆形指示
- `ViewModels/HistoryAlarmViewModel.cs`:查询异常路径接 DialogWindow
- settings.json:无改动 (CmdJogInFwd=ns=4;i=496、CmdJogInBwd=i=507、CmdJogOutFwd=i=518、CmdJogOutBwd=i=529、HMIHeart=Int/i=540 均已就位)
