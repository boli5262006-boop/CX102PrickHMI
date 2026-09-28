# Tasks

## 1. MainViewModel 搭接/复位/键盘逻辑

- [ ] 1.1 泵新增:SpliceValue1/2/3Value (double)、SpliceValue1/2/3OutOfRange、SpliceValue1/2/3Delta、HasAnySpliceAlarm、BannerText、StsNewRcpBlink;UpdateSpliceSnapshot 辅助;RefreshSpliceBanner
- [ ] 1.2 ResetCommand = AsyncRelayCommand(ResetPulseAsync):CmdReset true → 50ms → false
- [ ] 1.3 键盘首键替换:_keypadFreshInput;OpenKeypad/ClearKeypad 置位;AppendKeypadDigit 首键替换分支
- 验证:build 0 错误;泵无写操作

## 2. OverlapMonitorView 实点绑定与报警联动

- [ ] 2.1 横幅双态 (HasAnySpliceAlarm + BannerText)
- [ ] 2.2 三卡片:仪表 Value/GaugeColor 绑定、徽章、Δ、四角饰条、边框脉冲 (逐卡 DataTrigger)
- [ ] 2.3 示意图 mm 标签绑定;矩形保持静态
- 验证:build 0 错误;无残留硬编码 4.2/7.3/3.8

## 3. MonitorView 设置按钮闪烁 + MainWindow 复位按钮

- [ ] 3.1 设置按钮局部样式 DataTrigger StsNewRcpBlink → Primary
- [ ] 3.2 顶栏复位按钮 (HoldButtonStyle, ResetCommand)
- 验证:build 0 错误

## 4. 终验

- [ ] 4.1 全量构建 + 绑定名核对 + 硬编码扫描
