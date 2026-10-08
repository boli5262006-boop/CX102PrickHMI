# Proposal

## Why

HMI 安装在 10.1 寸屏 (1280×800) 上,操作员反映全项目字体偏小看不清——尤其搭接监控页 (徽章/标签 9-10 号、仪表 240×160)。另外监控页的校准按钮 (内侧校准/外侧校准,点位 CmdHomingIn/CmdHomingOut) 在此前的界面改版中被误删,需要恢复;搭接页底部"采样频率/标准范围/传感器"信息条对操作员无价值,且占用宝贵纵向空间,移除后可让三个仪表盘区域放大。

## What Changes

- 监控页第二行行首恢复两个校准按钮:**内侧校准** (`HoldHomingInCommand`→`CmdHomingIn`)、**外侧校准** (`HoldHomingOutCommand`→`CmdHomingOut`),按住式行为与样式不变;设置按钮与维修模式状态右移。
- 全项目字体定向放大 (方案 A,无全局缩放):`Controls.xaml` 基础 Control 14→16、表格字 13→15;主窗口标题 18→20、底栏/状态字 12→14;监控页标题 20→24、大数值 30→36、小标签 10/12→13/14;数字键盘 18→20;DialogWindow 14→16。
- 搭接监控页重排 (消除滚动条、大小刚好):
  - 移除外层 ScrollViewer,改为 Grid (标题 Auto + 内容 *),内容套 **Viewbox (Stretch=Uniform)** 等比适配任何窗口尺寸——保证不溢出、无滚动条、排版完整
  - 移除页脚信息条 (采样频率/标准范围/传感器)
  - 三个仪表改放入各自的 Viewbox,随卡片空间放大 (逻辑尺寸 240×160 等比放大);字号全面上调:徽章 13→16、位置名 12→15、卡片标题 14→18、mm 标签与 Δ 文本 10→14、示意图内文字随 Viewbox 自动放大
  - 页面标题 20→26

## Capabilities

### New Capabilities

- `overlap-ui-readability`: 搭接监控页 Viewbox 等比适配布局 (无滚动条/不溢出)、字号放大规范、页脚信息条移除。

### Modified Capabilities

（无——校准按钮恢复属于监控页操作面板的既有 capability 范围内的界面恢复,不新增行为契约;按钮行为 (按住写 CmdHomingIn/Out) 与既有实现完全一致。）

## Impact

- `Views/OverlapMonitorView.xaml`:布局重构 (ScrollViewer→Viewbox 适配)、字号放大、页脚移除
- `Views/MonitorView.xaml`:第二行行首加两个校准按钮;字号微调
- `styles/Controls.xaml`、`MainWindow.xaml`、`Views/HistoryAlarmView.xaml`、`Views/DialogWindow.xaml`:字号上调
- `ViewModels/MainViewModel.cs`:**零改动** (HoldHomingIn/OutCommand 已存在)
- settings.json:零改动 (CmdHomingIn/CmdHomingOut 已定义)
