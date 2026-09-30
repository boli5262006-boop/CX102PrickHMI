# Design

## Context

- 附加行为 `MomentaryButtonBehavior` + `SendMomentaryAsync(varName, value)` 点动写通道已就位 (MonitorView 现有 点动前进/点动后退 两钮绑旧点位 `CmdJogFwd/CmdJogBwd`)。
- PLCCOM 心跳循环已存在:`ReadHeartbeat()` 读 `CurrentValue["HMIHeart"]` 按 int 解析 (用户已改),`MaxHeartbeatFailures=4`、`heartbeatCheckInterval=1000`;PLCCOM 运行于线程池线程。
- HslArcGauge/键盘浮层均为页内/窗口级元素;本项目尚无可复用对话框组件。
- settings.json 已含 `CmdJogInFwd/InBwd/OutFwd/OutBwd` (Bool) 与 `HMIHeart` (Int);无需改 JSON。

## Decisions

- **D1 点动命令**:移除 `HoldJogFwdCommand/HoldJogBwdCommand`,新增 `HoldJogInFwdCommand/HoldJogInBwdCommand/HoldJogOutFwdCommand/HoldJogOutBwdCommand` (RelayCommand<bool> → SendMomentaryAsync,点位分别为 CmdJogInFwd/CmdJogInBwd/CmdJogOutFwd/CmdJogOutBwd)。
- **D2 IsCommAbnormal**:新增通知属性。PLCCOM (后台线程) 中判定:进入异常分支 (heartbeatFailureCount 达阈值) 且当前 false → 置 true;心跳值变化分支 → 置 false。赋值统一经 `Application.Current.Dispatcher.BeginInvoke` (弹窗打开/关闭必须 UI 线程,顺带保证通知线程正确)。
- **D3 DialogWindow 组件**:新 `Views/DialogWindow.xaml` (Window,WindowStyle=None,AllowsTransparency,Background 透明;内部深色卡片 Border Surface 底 + 2px 边框 + 红色呼吸 ColorAnimation 0.8s AutoReverse Forever;⚠ 图标 + 标题 + 副文 + 确定按钮)。构造参数 `(string title, string subtitle, bool showOk)`;确定按钮 Visibility 绑 showOk。Topmost=true、ShowInTaskbar=false、WindowStartupLocation=CenterScreen。
- **D4 通讯弹窗生命周期**:MainViewModel 持有 `_commDialog` 引用。IsCommAbnormal 上升沿:若 `_commDialog` 未开 → new DialogWindow("PLC 通讯异常","通讯恢复后此提示自动消失", showOk:false).Show() (非模态);下降沿:Dispatcher 上 Close() 并置空引用。上升沿仅在实例为空时触发 → 不重复弹。
- **D5 查询失败接入**:HistoryAlarmViewModel 查询逻辑 try/catch,catch 中经 Dispatcher `new DialogWindow("查询失败", ex.Message, showOk:true).ShowDialog()` (模态,带确定)。
- **D6 底栏**:MainWindow 底栏 FooterStatus TextBlock 替换为 `Text="PLC连接状态"` + 相邻 Ellipse(默认绿 Success,DataTrigger IsCommAbnormal=true → Error);Version 保留。`FooterStatus` 属性从 MainViewModel 移除 (避免死代码)。
- **D7 按钮布局**:MonitorView 按钮区改两行 StackPanel:第一行 4 个点动 (HoldJogInFwd/JogInBwd/JogOutFwd/JogOutBwd,文案 点动前进内侧/点动后退内侧/点动前进外侧/点动后退外侧);第二行 校准内侧/校准外侧/设置 (原样)。全部 HoldButtonStyle。

## Risks

- PLCCOM 后台线程直接操作 Window 会崩 → 所有 DialogWindow 打开/关闭必须经 Dispatcher (D2/D4 已定)。
- 连接完全断开时 ReadHeartbeat 走兜底返回 1 → 值冻结同样计入失败 → 异常判定天然覆盖断连场景。
- DialogWindow 置顶且无确定钮:恢复后必须确保 Close 被调用 (生命周期与 IsCommAbnormal 下降沿绑定;应用退出时 Window 随进程结束,可接受)。
