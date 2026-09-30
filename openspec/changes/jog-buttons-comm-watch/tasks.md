# Tasks

## 1. DialogWindow 缁勪欢

- [x] 1\.1 鏂板缓 `Views/DialogWindow.xaml` + `.xaml.cs`:鏋勯€?`(string title, string subtitle, bool showOk)`;WindowStyle=None + AllowsTransparency + 閫忔槑鑳屾櫙;鍗＄墖 Border (Surface 搴曘€?px #EF4444 杈规銆佸渾瑙?12銆丮inWidth 360) + 鈿?鍥炬爣 + 鏍囬 + 鍓枃 + 纭畾鎸夐挳 (Visibility 缁?showOk 鍙傛暟);Topmost銆丼howInTaskbar=false銆丆enterScreen;杈规绾㈣壊鍛煎惛 ColorAnimation 0.8s AutoReverse Forever;纭畾鎸夐挳 Click 鈫?Close()
- [x] 1\.2 csproj 娉ㄥ唽 Page/Compile (DependentUpon)
- 楠岃瘉:build 0 閿欒;code-behind 浠呮湁鏋勯€犱笌 OK_Click

## 2. MainViewModel:鐐瑰姩鍛戒护鏇挎崲 + 閫氳鏍囧織 + 寮圭獥鐢熷懡鍛ㄦ湡

- [x] 2\.1 绉婚櫎 HoldJogFwdCommand/HoldJogBwdCommand;鏂板 HoldJogInFwdCommand/HoldJogInBwdCommand/HoldJogOutFwdCommand/HoldJogOutBwdCommand (鐐逛綅 CmdJogInFwd/CmdJogInBwd/CmdJogOutFwd/CmdJogOutBwd)
- [x] 2\.2 鏂板 `IsCommAbnormal` (bool, SetProperty);PLCCOM 鍒ゅ畾澶勭粡 Dispatcher 璧嬪€?寮傚父涓婂崌娌挎墦寮€ `_commDialog`(Show,鏃犵‘瀹氶挳,涓嶉噸澶嶅脊),鎭㈠涓嬮檷娌?Close
- [x] 2\.3 绉婚櫎 `FooterStatus` 灞炴€?- 楠岃瘉:build 0 閿欒;鏃?CmdJogFwd/CmdJogBwd 娈嬬暀;PLCCOM 涔嬪鏃?IsCommAbnormal 鍐欏叆

## 3. MonitorView 涓よ鎸夐挳 + MainWindow 搴曟爮

- [x] 3\.1 鎸夐挳鍖轰袱琛?绗竴琛?鐐瑰姩鍓嶈繘鍐呬晶/鐐瑰姩鍚庨€€鍐呬晶/鐐瑰姩鍓嶈繘澶栦晶/鐐瑰姩鍚庨€€澶栦晶 (缁?HoldJogInFwd/JogInBwd/JogOutFwd/JogOutBwd);绗簩琛?鏍″噯鍐呬晶/鏍″噯澶栦晶/璁剧疆
- [x] 3\.2 搴曟爮:FooterStatus TextBlock 鈫?"PLC杩炴帴鐘舵€? + Ellipse (缁块粯璁?DataTrigger IsCommAbnormal=true 鈫?绾?;Version 淇濈暀
- 楠岃瘉:build 0 閿欒;鏃?FooterStatus 缁戝畾娈嬬暀

## 4. HistoryAlarmViewModel 鏌ヨ澶辫触寮规

- [x] 4\.1 鏌ヨ閫昏緫 try/catch 鈫?catch 缁?Dispatcher `new DialogWindow("鏌ヨ澶辫触", 鎽樿, true).ShowDialog()`
- 楠岃瘉:build 0 閿欒

## 5. 缁堥獙

- [x] 5\.1 鍏ㄩ噺鏋勫缓 0 閿欒;grep 鏃?CmdJogFwd/CmdJogBwd/FooterStatus 娈嬬暀;娉垫棤鍐欐搷浣滀笉鍙?