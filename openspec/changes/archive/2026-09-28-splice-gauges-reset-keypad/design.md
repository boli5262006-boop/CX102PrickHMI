# Design

## Context

单 MainViewModel (SetProperty 手写通知) + 500ms CurrentValue 快照泵 + ValueLock 互斥已就位。HslArcGauge 的 Value/GaugeColor/SettingValue/Min/Max 均为依赖属性,支持绑定与样式触发 (反射已验证)。settings.json 已含 CmdReset (Bool, ns=4;i=344) 与 SpliceValue1/2/3 (Float, ns=4;i=441/452/463)。

## Decisions

- **D1 超限判定**: `v < 2.0 || v > 6.0` (4±2 含边界内),Double 精度。
- **D2 泵新增快照**: SpliceValue1/2/3Value (double, 初始 0);SpliceValue1/2/3OutOfRange (bool);SpliceValue1/2/3Delta (string, `(v-4).ToString("+0.0;-0.0;0.0")` + 超限追加 `" 超限"`);StsNewRcpBlink (bool);HasAnySpliceAlarm (bool,setter 联动通知 BannerText);BannerText (计算属性:正常 `检测正常 | SYSTEM NORMAL`,报警 `报警 | ALARM: <位号、...> 搭接量超出标准范围 (4±2mm)`,位号 P-01 前段/P-02 中段/P-03 后段)。
- **D3 CmdReset**: ResetCommand = AsyncRelayCommand;写 true → Task.Delay(50) → 写 false;不防抖 (连续点击时各发各的脉冲,最终态恒为 false);失败走 CalibrationMessage + NLog。
- **D4 键盘首键替换**: `_keypadFreshInput` 标志;OpenKeypad 与 C 清空置 true;首键数字/点整体替换 (`.` → `0.`),之后按原规则追加 (单小数点、长度 10)。
- **D5 Overlap XAML**: 三卡片样式同构,逐卡 DataTrigger (SpliceValueXOutOfRange) 驱动:卡片边框脉冲 (EnterActions/ExitActions + StopStoryboard + ObjectAnimationUsingKeyFrames 清 Effect)、徽章色/文本、GaugeColor、Δ 前景、四角饰条 (卡片内隐式 Path 样式)。横幅由 HasAnySpliceAlarm 驱动双态。示意图矩形保持静态 (Q4=A),仅 mm 标签绑定 `SpliceValueXValue` + `StringFormat={}{0:F1}mm`,前景随报警态翻转。
- **D6 设置按钮**: 局部 Style BasedOn HoldButtonStyle,DataTrigger StsNewRcpBlink=True → Background Primary;点动行为不变。
- **D7 复位按钮**: MainWindow 顶栏状态 StackPanel 内追加,Content=复位,HoldButtonStyle,Padding 16,6。

## Risks

- HslArcGauge.Value 绑定 Double,泵缺键时保持上次值 (与既有约定一致)。
- ExitActions 用 ObjectAnimationUsingKeyFrames 将 Effect 置 {x:Null},避免脉冲残留。
- Storyboard x:Name 需逐卡唯一 (Pulse1/2/3、BannerPulse)。
