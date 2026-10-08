# UI Enlarge / Calibrate Buttons — Specs

## ADDED Requirements

### Requirement: 校准按钮恢复
监控页第二行行首 SHALL 恢复两个按住式校准按钮:**内侧校准** (`HoldHomingInCommand`→`CmdHomingIn`)、**外侧校准** (`HoldHomingOutCommand`→`CmdHomingOut`),样式与点动按钮一致 (HoldButtonStyle);设置按钮与维修模式状态指示顺移至其后。

#### Scenario: 校准按钮点动写入
- **WHEN** 按住 "内侧校准"
- **THEN** PLC `CmdHomingIn` 收到 true;松开后收到 false
- **WHEN** 按住 "外侧校准"
- **THEN** PLC `CmdHomingOut` 收到 true;松开后收到 false

#### Scenario: 第二行顺序
- **WHEN** 查看监控页按钮区第二行
- **THEN** 顺序为 内侧校准、外侧校准、设置、维修模式状态(未激活/激活)

### Requirement: 搭接页 Viewbox 等比适配 (无滚动条)
搭接监控页 SHALL 移除外层 ScrollViewer,改为 Grid(标题 Auto + 内容 *) 且内容套 Viewbox (Stretch=Uniform):三张卡片整体按可用空间等比缩放,任何窗口尺寸下内容完整可见——不出现滚动条、不发生裁剪或溢出、排版比例保持一致。页脚信息条 (采样频率/标准范围/传感器) SHALL 移除。

#### Scenario: 目标分辨率恰好铺满
- **WHEN** 在 1280×800 全屏打开搭接监控页
- **THEN** 三张卡片与标题完整显示,无滚动条、无裁剪,仪表与文字明显大于改版前

#### Scenario: 窗口尺寸变化仍适配
- **WHEN** 窗口被调整为更大或更小
- **THEN** 内容按比例缩放,始终完整可见

### Requirement: 搭接页与全局字号放大
字号 SHALL 定向放大:搭接监控页 页面标题 20→26、卡片标题 14→18、徽章 13→16、位置名 12→15、mm 标签与 Δ 文本 10→14、示意图内文字 9→12 (随 Viewbox 放大);全局 `Controls.xaml` 基础 Control 14→16、表格字 13→15;主窗口标题 18→20、底栏与状态文字 12→14;监控页大数值 30→36、小标签 10/12→13/14;数字键盘按键 18→20;DialogWindow 14→16。点位绑定与业务逻辑不受影响。

#### Scenario: 搭接页字号
- **WHEN** 打开搭接监控页
- **THEN** 仪表读数、徽章、Δ 文本、标题的字号均不小于上述目标值,无内容溢出

#### Scenario: 其他页面字号
- **WHEN** 打开监控页与历史报警页
- **THEN** 关键标签与表格文字达到放大后的目标值,布局无重叠
