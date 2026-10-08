# Design

## Context

- 10.1 寸目标屏 1280×800;主窗口 Height=800 Width=1280 (MinHeight 700 / MinWidth 1100)。
- 全部字号为 XAML 显式值 (10/11/12/13/14/16/18/20/30),无统一缩放机制;搭接页外层是 ScrollViewer (内容超高时出滚动条)。
- 校准按钮的 VM 命令 `HoldHomingInCommand/HoldHomingOutCommand` (CmdHomingIn/Out) 仍存在于 MainViewModel,仅 XAML 按钮此前被移除——恢复即可,零 VM 改动。
- 搭接页卡片内的示意图本就是 Viewbox (随卡片放大自动缩放内部文字)。

## Decisions

- **D1 布局适配**:搭接页移除 ScrollViewer;根 Grid = 标题行 (Auto) + 内容行 (*)。内容行套 `Viewbox Stretch=Uniform`(横向拉伸对齐 Stretch),内部为**固定设计尺寸画布** `UniformGrid Columns=3 Width=1232 Height=545` 承载三张卡片。Viewbox 等比缩放保证:任意窗口尺寸下三卡完整可见、无滚动条、无溢出、比例恒定。
- **D2 卡内仪表放大**:HslArcGauge (240×160 逻辑尺寸) 包一层 `Viewbox Stretch=Uniform` 填满卡片中部剩余空间 (约 340×230),仪表与读数随之放大;卡片结构:徽章行 (顶部固定) → 仪表 Viewbox (*) → 示意图 Viewbox (固定高) → Δ 行 (底部固定)。
- **D3 字号规范** (定向,非全局缩放):
  - 搭接页:页面标题 20→26;卡片标题 14→18;徽章文字 13→16、位置名 12→15;示意图分隔标签 10→13;mm 标签与 Δ 文本 10→14;NORMAL/ALARM 10→13
  - 全局:Controls.xaml 基础 Control 14→16、NavButtonStyle 14→16、TableHeader/TableCell 13→15、InputStyle 13→15
  - 主窗口:头部标题 18→20;SystemStatus/PLC连接状态/Version 12→14
  - 监控页:页面标题 20→24;配方/实际值大数字 30→36;小标签 10/12→13/14;CalibrationMessage 12→14
  - HistoryAlarmView:DateSummary 11→14 (其余走全局表格样式)
  - NumericKeypadOverlay:按键 18→20、读数 30→34、MinHeight 52→58
  - DialogWindow:正文 14→16
- **D4 校准按钮恢复**:第二行行首插入 内侧校准/外侧校准 (HoldButtonStyle + MomentaryButtonBehavior → HoldHomingInCommand/HoldHomingOutCommand),设置按钮与维修模式状态 TextBlock 顺移其后;命名按用户措辞 (内侧校准/外侧校准)。
- **D5 页脚移除**:删除 Grid.Row=2 的信息条 Border;释放高度并入内容行。

## Risks

- Viewbox 等比缩放在不同长宽比下留白:Stretch=Uniform 保证完整可见,留白均匀可接受 (目标屏比例固定)。
- HslArcGauge 在 Viewbox 内的交互 (无交互,纯显示) 不受影响。
- 全局基础字号 14→16 会影响所有未显式指定 FontSize 的控件——逐视图抽查按钮/表格不破版。
