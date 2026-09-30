# Tasks

## 1. MainViewModel 鍏娴佺▼ + 鎶ヨ婧愭帴鍏?
- [x] 1.1 鏋勯€犲嚱鏁?`Steps` 鎾 6 涓?`ProcessStep`(涓荤嚎鍋滄満/閰嶆柟鍔犺浇/妲借緤鎵撳紑/浣嶇疆鍒拌揪/浼犻€佸甫鍥為€€/妲借緤鏀跺洖,鍒濆 Pending/"鏈紑濮?,缂栧彿 1-6)
- [x] 1.2 娉垫柊澧?6 涓楠ょ偣浣嶅揩鐓?`MainMachineStop`/`StsStepRcpLoaded`/`StsStepRollerOpen`/`StsStepAxisInPos`/`StsStepConveyorDone`/`StsStepRollerClosed` 鈫?鎸?`UpdateState(Completed/"宸插畬鎴? 鎴?Pending/"鏈紑濮?)` 鍒锋柊瀵瑰簲姝ラ;`MainMachineStop=true` 鈫?Completed
- [x] 1.3 娉垫柊澧炴姤璀︽簮:13 涓姤璀︾偣浣?(10 楂樻姤璀?true=鎶ヨ,3 浣庢姤璀?true=鎶ヨ) 浠讳竴涓虹湡 鈫?`IsAlarmActive=true`;鍏ㄩ儴涓哄亣 鈫?false銆俙IsAlarmActive` 鏀逛负 backing field + SetProperty (鏇挎崲鎭?false 鐨?get-only)
- [x] 1.4 纭 `ProcessStep.UpdateState` 鏈夌姸鎬佸垽閲?鑻ユ棤,鍦ㄦ车璋冪敤渚у垽閲?- 楠岃瘉:`dotnet build` 0 閿欒;娉垫棤鍐欐搷浣?鏃犱换浣曟楠よ繘鍏?Error 鎬?
## 2. 楠岃瘉

- [x] 2.1 grep 纭鏃犳柊澧炵‖缂栫爜妯℃嫙鍊?Steps 鍒濆鍖栦负 6 涓湡瀹炴爣棰樼殑姝ラ
- [x] 2.2 鏋勫缓 0 閿欒;MonitorView/MainWindow/settings.json 鏃犳敼鍔?