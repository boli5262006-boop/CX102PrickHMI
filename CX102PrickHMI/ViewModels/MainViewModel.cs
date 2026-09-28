using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CX102PrickHMI.Interfaces;
using CX102PrickHMI.Models;
using CX102PrickHMI.Services;
using CX102PrickHMI.Utilities;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using thinger.CommunicationLib;
using thinger.ConfigLib;
using thinger.DataConvertLib;

namespace CX102PrickHMI.ViewModels
{
    public enum HmiPage
    {
        Monitor,
        Overlap,
        Alarms,
        History
    }

    public enum KeypadTarget
    {
        None,
        In,
        Out
    }

    public sealed class MainViewModel : ObservableObject
    {
        private object _currentPage;
        private HmiPage _selectedPage;

        private IAlarmRepository alarmservice;
        public MainViewModel(IAlarmRepository alarms,
            Views.MonitorView monitorView,
            Views.OverlapMonitorView overlapView,
            Views.AlarmListView alarmListView,
            Views.HistoryAlarmView historyView)
        {

            alarmservice=alarms;
            //alarmservice.Insert(new Models.Alarms()
            //{
            //    InsertTime=DateTime.Now,
            //    Symbol="Test",
            //    AlarmState="鍒拌揪",
            //    AlarmNote = "Test",
            //    VarName = "limitswitch"
            //});

            Steps = new ObservableCollection<ProcessStep>();
            AlarmItems = new ObservableCollection<AlarmEntry>();
            HistoryItems = new ObservableCollection<AlarmEntry>();

            // 椤甸潰瑙嗗浘涓哄鍣ㄥ崟渚嬶紝鍒囨崲鏃剁洿鎺ュ鐢ㄥ凡鏋勫缓鐨勮瑙夋爲锛岄伩鍏嶉噸澶嶅疄渚嬪寲閫犳垚鍗￠】
            _monitorPage = monitorView;
            _overlapPage = overlapView;
            _alarmsPage = alarmListView;
            _historyPage = historyView;

            // 鍥涗釜椤甸潰鐨?DataContext 缁熶竴鎸囧悜 MainViewModel
            _monitorPage.DataContext = this;
            _overlapPage.DataContext = this;
            _alarmsPage.DataContext = this;
            _historyPage.DataContext = this;

            _currentPage = _monitorPage;
            _selectedPage = HmiPage.Monitor;

            ShowMonitorCommand = new RelayCommand(() => SelectPage(HmiPage.Monitor, _monitorPage));
            ShowOverlapCommand = new RelayCommand(() => SelectPage(HmiPage.Overlap, _overlapPage));
            ShowAlarmsCommand = new RelayCommand(() => SelectPage(HmiPage.Alarms, _alarmsPage));
            ShowHistoryCommand = new RelayCommand(() => SelectPage(HmiPage.History, _historyPage));
            ExitCommand = new RelayCommand(() => Application.Current.Shutdown());

            OpenKeypadInCommand = new RelayCommand(() => OpenKeypad(KeypadTarget.In));
            OpenKeypadOutCommand = new RelayCommand(() => OpenKeypad(KeypadTarget.Out));
            KeypadDigitCommand = new RelayCommand<string>(AppendKeypadDigit);
            KeypadBackspaceCommand = new RelayCommand(KeypadBackspace);
            KeypadClearCommand = new RelayCommand(ClearKeypad);
            ConfirmKeypadCommand = new AsyncRelayCommand(ConfirmKeypadAsync);
            CancelKeypadCommand = new RelayCommand(CancelKeypad);
            HoldHomingInCommand = new RelayCommand<bool>(value => _ = SendMomentaryAsync("CmdHomingIn", value));
            HoldHomingOutCommand = new RelayCommand<bool>(value => _ = SendMomentaryAsync("CmdHomingOut", value));
            HoldSetupCommand = new RelayCommand<bool>(value => _ = SendMomentaryAsync("CmdSetup", value));
            ResetCommand = new AsyncRelayCommand(ResetPulseAsync);

            var result = GetDeviceByPath(xmlPath);
            if (result.IsSuccess)
            {
                CommonMethods.plcDevice = result.Content;
                CommonMethods.plcDevice.Cts = new CancellationTokenSource();
                CommonMethods.plcDevice.ServerUrl = "opc.tcp://192.168.3.1:4840";
                CommonMethods.plc.UseSecurity = true;
                CommonMethods.plcDevice.Init();
                CommonMethods.plcDevice.AlarmTriggerEvent += PlcDevice_AlarmTriggerEvent;
                CommonMethods.plcDevice.Cts = new CancellationTokenSource();
                updateTimer = new DispatcherTimer();
                updateTimer.Interval = TimeSpan.FromMilliseconds(500);
                updateTimer.Tick += UpdateTimer_Tick;
                updateTimer.Start();
                firstConnect = true;
                Task.Run(async () =>
                {
                    await PLCCOM(CommonMethods.plcDevice, CommonMethods.plc);
                }, CommonMethods.plcDevice.Cts.Token);

            }
        }

        // CurrentValue 璇诲啓浜掓枼閿侊細鍚庡彴杞鍐?/ UI 娉佃鍏辩敤
        private static readonly object ValueLock = new object();

        // 500ms 蹇収娉碉細鍙 CurrentValue 鍒锋柊鐣岄潰灞炴€э紝缁濅笉鍐?PLC
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            var device = CommonMethods.plcDevice;
            if (device == null || device.CurrentValue == null)
            {
                return;
            }

            var values = device.CurrentValue;

            UpdateTextSnapshot(values, "RcpActIn", v => RecipeRing1 = v);
            UpdateTextSnapshot(values, "RcpActOut", v => RecipeRing2 = v);
            UpdateTextSnapshot(values, "ActPosIn", v => ActualRing1 = v);
            UpdateTextSnapshot(values, "ActPosOut", v => ActualRing2 = v);

            UpdateBoolSnapshot(values, "StsHomingDoneIn", v => StsHomingDoneIn = v);
            UpdateBoolSnapshot(values, "StsHomingDoneOut", v => StsHomingDoneOut = v);
            UpdateBoolSnapshot(values, "DriveInEnable", v => DriveInEnable = v);
            UpdateBoolSnapshot(values, "DriveOutEnable", v => DriveOutEnable = v);
            UpdateBoolSnapshot(values, "MainMachineStop", v => MainMachineStop = v);
            UpdateBoolSnapshot(values, "TriggerMaintenance", v => TriggerMaintenance = v);

            UpdateSpliceSnapshot(values, "SpliceValue1", v => SpliceValue1Value = v, v => SpliceValue1Delta = v, v => SpliceValue1OutOfRange = v);
            UpdateSpliceSnapshot(values, "SpliceValue2", v => SpliceValue2Value = v, v => SpliceValue2Delta = v, v => SpliceValue2OutOfRange = v);
            UpdateSpliceSnapshot(values, "SpliceValue3", v => SpliceValue3Value = v, v => SpliceValue3Delta = v, v => SpliceValue3OutOfRange = v);
            UpdateBoolSnapshot(values, "StsNewRcpBlink", v => StsNewRcpBlink = v);
            RefreshSpliceBanner();


            // 鍘熺偣鍊艰緭鍏ユ锛氬搴旇酱閿洏鎵撳紑鏈熼棿鏆傚仠鍒锋柊锛岄伩鍏嶇紪杈戝€艰瑕嗙洊
            if (KeypadTarget != KeypadTarget.In)
            {
                UpdateTextSnapshot(values, "HomingRefPosIn", v => HomingRefPosInText = v);
            }
            if (KeypadTarget != KeypadTarget.Out)
            {
                UpdateTextSnapshot(values, "HomingRefPosOut", v => HomingRefPosOutText = v);
            }
        }

        private static void UpdateTextSnapshot(Dictionary<string, object> values, string key, Action<string> assign)
        {
            object raw;
            bool found;
            lock (ValueLock)
            {
                found = values.TryGetValue(key, out raw);
            }

            if (!found)
            {
                return; // 缂洪敭/闈炴硶鍊间繚鐣欎笂娆″€?
            }

            var text = FormatF2(raw);
            if (text != null)
            {
                assign(text);
            }
        }

        private static void UpdateBoolSnapshot(Dictionary<string, object> values, string key, Action<bool> assign)
        {
            object raw;
            bool found;
            lock (ValueLock)
            {
                found = values.TryGetValue(key, out raw);
            }

            if (!found)
            {
                return; // 缂洪敭/闈炴硶鍊间繚鐣欎笂娆″€?
            }

            assign(ParseBool(raw));
        }

        private static string FormatF2(object raw)
        {
            float parsed;
            if (raw != null && float.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed.ToString("F2", CultureInfo.InvariantCulture);
            }

            return null;
        }

        private static bool ParseBool(object raw)
        {
            return raw is bool b ? b : (bool.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), out var parsed) && parsed);
        }

        private void UpdateSpliceSnapshot(Dictionary<string, object> values, string key, Action<double> assignValue, Action<string> assignDelta, Action<bool> assignOutOfRange)
        {
            object raw;
            bool found;
            lock (ValueLock)
            {
                found = values.TryGetValue(key, out raw);
            }
            if (!found) return;
            float f;
            if (!float.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out f)) return;
            var v = (double)f;
            assignValue(v);
            var outOfRange = v < 2.0 || v > 6.0;
            assignOutOfRange(outOfRange);
            assignDelta((v - 4.0).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + (outOfRange ? " 瓒呴檺" : ""));
        }

        private void RefreshSpliceBanner()
        {
            HasAnySpliceAlarm = SpliceValue1OutOfRange || SpliceValue2OutOfRange || SpliceValue3OutOfRange;
        }

        public object CurrentPage
        {
            get { return _currentPage; }
            private set { SetProperty(ref _currentPage, value); }
        }

        public HmiPage SelectedPage
        {
            get { return _selectedPage; }
            private set
            {
                if (SetProperty(ref _selectedPage, value))
                {
                    OnPropertyChanged(nameof(IsMonitorSelected));
                    OnPropertyChanged(nameof(IsOverlapSelected));
                    OnPropertyChanged(nameof(IsAlarmsSelected));
                    OnPropertyChanged(nameof(IsHistorySelected));
                }
            }
        }

        public bool IsMonitorSelected { get { return SelectedPage == HmiPage.Monitor; } }
        public bool IsOverlapSelected { get { return SelectedPage == HmiPage.Overlap; } }
        public bool IsAlarmsSelected { get { return SelectedPage == HmiPage.Alarms; } }
        public bool IsHistorySelected { get { return SelectedPage == HmiPage.History; } }

        private readonly Views.MonitorView _monitorPage;
        private readonly Views.OverlapMonitorView _overlapPage;
        private readonly Views.AlarmListView _alarmsPage;
        private readonly Views.HistoryAlarmView _historyPage;

        // 鈥斺€?鐩戞帶椤碉紙PLC 缁戝畾灞炴€э級 鈥斺€?
        public ObservableCollection<ProcessStep> Steps { get; private set; }

        // 璇诲€煎揩鐓у睘鎬э細鐢?UpdateTimer_Tick 浠?CurrentValue 鍒锋柊
        private string _recipeRing1 = "0.00";
        public string RecipeRing1
        {
            get { return _recipeRing1; }
            private set { SetProperty(ref _recipeRing1, value); }
        }

        private string _recipeRing2 = "0.00";
        public string RecipeRing2
        {
            get { return _recipeRing2; }
            private set { SetProperty(ref _recipeRing2, value); }
        }

        private string _actualRing1 = "0.00";
        public string ActualRing1
        {
            get { return _actualRing1; }
            private set { SetProperty(ref _actualRing1, value); }
        }

        private string _actualRing2 = "0.00";
        public string ActualRing2
        {
            get { return _actualRing2; }
            private set { SetProperty(ref _actualRing2, value); }
        }

        private string _homingRefPosInText = "0.00";
        public string HomingRefPosInText
        {
            get { return _homingRefPosInText; }
            private set { SetProperty(ref _homingRefPosInText, value); }
        }

        private string _homingRefPosOutText = "0.00";
        public string HomingRefPosOutText
        {
            get { return _homingRefPosOutText; }
            private set { SetProperty(ref _homingRefPosOutText, value); }
        }

        private bool _stsHomingDoneIn;
        public bool StsHomingDoneIn
        {
            get { return _stsHomingDoneIn; }
            private set { SetProperty(ref _stsHomingDoneIn, value); }
        }

        private bool _stsHomingDoneOut;
        public bool StsHomingDoneOut
        {
            get { return _stsHomingDoneOut; }
            private set { SetProperty(ref _stsHomingDoneOut, value); }
        }

        private bool _driveInEnable;
        public bool DriveInEnable
        {
            get { return _driveInEnable; }
            private set { SetProperty(ref _driveInEnable, value); }
        }

        private bool _driveOutEnable;
        public bool DriveOutEnable
        {
            get { return _driveOutEnable; }
            private set { SetProperty(ref _driveOutEnable, value); }
        }

        // 璇箟锛歠alse = 杩愯涓紝true = 宸插仠鏈?
        private bool _mainMachineStop;
        public bool MainMachineStop
        {
            get { return _mainMachineStop; }
            private set { SetProperty(ref _mainMachineStop, value); }
        }

        private bool _triggerMaintenance;
        public bool TriggerMaintenance
        {
            get { return _triggerMaintenance; }
            private set { SetProperty(ref _triggerMaintenance, value); }
        }

        private string _servo1Actual = "0.00";
        private string _servo2Actual = "0.00";
        public string Servo1Actual
        {
            get { return _servo1Actual; }
            set { SetProperty(ref _servo1Actual, value); }
        }
        public string Servo2Actual
        {
            get { return _servo2Actual; }
            set { SetProperty(ref _servo2Actual, value); }
        }

        // 鍘熺偣杈撳叆妗嗘牎楠屽け璐ユ爣璁帮紙绾㈣壊杈规锛?
        private bool _homingRefPosInInvalid;
        public bool HomingRefPosInInvalid
        {
            get { return _homingRefPosInInvalid; }
            private set { SetProperty(ref _homingRefPosInInvalid, value); }
        }

        private bool _homingRefPosOutInvalid;
        public bool HomingRefPosOutInvalid
        {
            get { return _homingRefPosOutInvalid; }
            private set { SetProperty(ref _homingRefPosOutInvalid, value); }
        }

        private string _calibrationMessage = "";
        public string CalibrationMessage
        {
            get { return _calibrationMessage; }
            private set { SetProperty(ref _calibrationMessage, value); }
        }

        // 报警脉冲本轮恒为关（原 AlarmListViewModel 合并处，本轮用不到）
        public bool IsAlarmActive
        {
            get { return false; }
        }

        // 鈥斺€?鍘熺偣鏁板瓧閿洏鐘舵€?鈥斺€?
        private KeypadTarget _keypadTarget;
        public KeypadTarget KeypadTarget
        {
            get { return _keypadTarget; }
            private set
            {
                if (SetProperty(ref _keypadTarget, value))
                {
                    OnPropertyChanged(nameof(KeypadVisible));
                    OnPropertyChanged(nameof(KeypadTargetText));
                }
            }
        }

        public bool KeypadVisible
        {
            get { return KeypadTarget != KeypadTarget.None; }
        }

        public string KeypadTargetText
        {
            get
            {
                switch (KeypadTarget)
                {
                    case KeypadTarget.In:
                        return "内侧伺服 校准原点值";
                    case KeypadTarget.Out:
                        return "外侧伺服 校准原点值";
                    default:
                        return "";
                }
            }
        }

        // 棣栭敭鏇挎崲鏍囧織锛氭墦寮€閿洏鎴?C 娓呯┖鍚庣疆 true锛屼笅涓€涓暟瀛?鐐规暣浣撴浛鎹㈣緭鍏ョ紦鍐?
        private bool _keypadFreshInput;

        private string _keypadEditValue = "";
        public string KeypadEditValue
        {
            get { return _keypadEditValue; }
            private set { SetProperty(ref _keypadEditValue, value); }
        }

        private string _keypadErrorText = "";
        public string KeypadErrorText
        {
            get { return _keypadErrorText; }
            private set
            {
                if (SetProperty(ref _keypadErrorText, value))
                {
                    OnPropertyChanged(nameof(KeypadHasError));
                }
            }
        }

        public bool KeypadHasError
        {
            get { return !string.IsNullOrEmpty(KeypadErrorText); }
        }

        // 鈥斺€?閿洏 / 鐐瑰姩鍛戒护 鈥斺€?
        public IRelayCommand<string> KeypadDigitCommand { get; }
        public IRelayCommand KeypadBackspaceCommand { get; }
        public IRelayCommand KeypadClearCommand { get; }
        public IRelayCommand ConfirmKeypadCommand { get; }
        public IRelayCommand CancelKeypadCommand { get; }
        public IRelayCommand OpenKeypadInCommand { get; }
        public IRelayCommand OpenKeypadOutCommand { get; }
        public IRelayCommand<bool> HoldHomingInCommand { get; }
        public IRelayCommand<bool> HoldHomingOutCommand { get; }
        public IRelayCommand<bool> HoldSetupCommand { get; }
        public IRelayCommand ResetCommand { get; }

        private void OpenKeypad(KeypadTarget target)
        {
            if (target == KeypadTarget.None)
            {
                return;
            }

            KeypadTarget = target;
            KeypadEditValue = target == KeypadTarget.In ? HomingRefPosInText : HomingRefPosOutText;
            _keypadFreshInput = true;
            KeypadErrorText = "";
            if (target == KeypadTarget.In)
            {
                HomingRefPosInInvalid = false;
            }
            else
            {
                HomingRefPosOutInvalid = false;
            }
        }

        private void ClearKeypad()
        {
            KeypadEditValue = "";
            _keypadFreshInput = true;
        }

        private void AppendKeypadDigit(string digit)
        {
            if (string.IsNullOrEmpty(digit))
            {
                return;
            }
            if (_keypadFreshInput)
            {
                _keypadFreshInput = false;
                KeypadEditValue = digit == "." ? "0." : digit;
                return;
            }
            if (KeypadEditValue.Length >= 10)
            {
                return;
            }
            if (digit == "." && KeypadEditValue.Contains("."))
            {
                return;
            }
            KeypadEditValue += digit;
        }

        private void KeypadBackspace()
        {
            if (KeypadEditValue.Length > 0)
            {
                KeypadEditValue = KeypadEditValue.Substring(0, KeypadEditValue.Length - 1);
            }
        }

        private void CancelKeypad()
        {
            CloseKeypad();
            HomingRefPosInInvalid = false;
            HomingRefPosOutInvalid = false;
        }

        private void CloseKeypad()
        {
            KeypadTarget = KeypadTarget.None;
            KeypadEditValue = "";
            KeypadErrorText = "";
        }

        private void SetKeypadInvalid(bool invalid)
        {
            if (KeypadTarget == KeypadTarget.In)
            {
                HomingRefPosInInvalid = invalid;
            }
            else if (KeypadTarget == KeypadTarget.Out)
            {
                HomingRefPosOutInvalid = invalid;
            }
        }

        private async Task ConfirmKeypadAsync()
        {
            try
            {
                float value;
                if (!float.TryParse(KeypadEditValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    KeypadErrorText = "请输入有效数字";
                    SetKeypadInvalid(true);
                    return;
                }

                if (value <= 0f)
                {
                    KeypadErrorText = "鍘熺偣鍊煎繀椤诲ぇ浜?0";
                    SetKeypadInvalid(true);
                    return;
                }

                value = (float)Math.Round((double)value, 2);

                var varName = KeypadTarget == KeypadTarget.In ? "HomingRefPosIn" : "HomingRefPosOut";
                string address;
                if (!PlcTagLookup.TryGetAddress(CommonMethods.plcDevice, varName, out address))
                {
                    CalibrationMessage = "鏈壘鍒扮偣浣嶅湴鍧€";
                    CloseKeypad();
                    return;
                }

                if (!await CommonMethods.plc.WriteNodeAsync<float>(address, value))
                {
                    // 鍐欏叆澶辫触锛氶敭鐩樹繚鎸佹墦寮€锛屽厑璁搁噸璇?
                    CalibrationMessage = "写入失败，请检查 PLC 连接";
                    return;
                }

                var text = value.ToString("F2", CultureInfo.InvariantCulture);
                if (KeypadTarget == KeypadTarget.In)
                {
                    HomingRefPosInText = text;
                    HomingRefPosInInvalid = false;
                }
                else
                {
                    HomingRefPosOutText = text;
                    HomingRefPosOutInvalid = false;
                }

                CloseKeypad();
                CalibrationMessage = "原点值已写入";
            }
            catch (Exception ex)
            {
                CalibrationMessage = "写入失败，请检查 PLC 连接";
                NLogHelper.Warn("原点值写入失败", ex);
            }
        }

        // 点动命令：按下 true / 松开或失去捕获 false
        private async Task SendMomentaryAsync(string varName, bool value)
        {
            try
            {
                string address;
                if (!PlcTagLookup.TryGetAddress(CommonMethods.plcDevice, varName, out address))
                {
                    CalibrationMessage = "鏈壘鍒扮偣浣嶅湴鍧€: " + varName;
                    return;
                }

                if (!await CommonMethods.plc.WriteNodeAsync<bool>(address, value))
                {
                    CalibrationMessage = "命令写入失败: " + varName;
                }
            }
            catch (Exception ex)
            {
                CalibrationMessage = "命令写入失败: " + varName;
                NLogHelper.Warn("命令写入失败: " + varName, ex);
            }
        }

        // 澶嶄綅锛氫竴娆＄偣鍑诲彂 true锛?0ms 鍚庡彂 false
        private async Task ResetPulseAsync()
        {
            try
            {
                string address;
                if (!PlcTagLookup.TryGetAddress(CommonMethods.plcDevice, "CmdReset", out address))
                {
                    CalibrationMessage = "鏈壘鍒扮偣浣嶅湴鍧€: CmdReset";
                    return;
                }
                if (!await CommonMethods.plc.WriteNodeAsync<bool>(address, true))
                {
                    CalibrationMessage = "澶嶄綅鍐欏叆澶辫触";
                    return;
                }
                await Task.Delay(100);
                if (!await CommonMethods.plc.WriteNodeAsync<bool>(address, false))
                {
                    CalibrationMessage = "澶嶄綅鍐欏叆澶辫触";
                }
            }
            catch (Exception ex)
            {
                CalibrationMessage = "澶嶄綅鍐欏叆澶辫触";
                NLogHelper.Warn("澶嶄綅鍛戒护鍐欏叆澶辫触", ex);
            }
        }

        // 鈥斺€?鎶ヨ璁板綍椤碉紙鍘?AlarmListViewModel 鍚堝苟锛?鈥斺€?
        public ObservableCollection<AlarmEntry> AlarmItems { get; private set; }

        // 鈥斺€?鍘嗗彶鎶ヨ椤碉紙鍘?HistoryAlarmViewModel 鍚堝苟锛?鈥斺€?
        public ObservableCollection<AlarmEntry> HistoryItems { get; private set; }
        public string DateSummary { get { return ""; } }

        // 鈥斺€?鎼帴鐩戞帶椤?(OverlapMonitorView) 鈥斺€?
        private double _spliceValue1Value;
        public double SpliceValue1Value
        {
            get { return _spliceValue1Value; }
            private set { SetProperty(ref _spliceValue1Value, value); }
        }

        private double _spliceValue2Value;
        public double SpliceValue2Value
        {
            get { return _spliceValue2Value; }
            private set { SetProperty(ref _spliceValue2Value, value); }
        }

        private double _spliceValue3Value;
        public double SpliceValue3Value
        {
            get { return _spliceValue3Value; }
            private set { SetProperty(ref _spliceValue3Value, value); }
        }

        private bool _spliceValue1OutOfRange;
        public bool SpliceValue1OutOfRange
        {
            get { return _spliceValue1OutOfRange; }
            private set { SetProperty(ref _spliceValue1OutOfRange, value); }
        }

        private bool _spliceValue2OutOfRange;
        public bool SpliceValue2OutOfRange
        {
            get { return _spliceValue2OutOfRange; }
            private set { SetProperty(ref _spliceValue2OutOfRange, value); }
        }

        private bool _spliceValue3OutOfRange;
        public bool SpliceValue3OutOfRange
        {
            get { return _spliceValue3OutOfRange; }
            private set { SetProperty(ref _spliceValue3OutOfRange, value); }
        }

        private string _spliceValue1Delta = "";
        public string SpliceValue1Delta
        {
            get { return _spliceValue1Delta; }
            private set { SetProperty(ref _spliceValue1Delta, value); }
        }

        private string _spliceValue2Delta = "";
        public string SpliceValue2Delta
        {
            get { return _spliceValue2Delta; }
            private set { SetProperty(ref _spliceValue2Delta, value); }
        }

        private string _spliceValue3Delta = "";
        public string SpliceValue3Delta
        {
            get { return _spliceValue3Delta; }
            private set { SetProperty(ref _spliceValue3Delta, value); }
        }

        private bool _stsNewRcpBlink;
        public bool StsNewRcpBlink
        {
            get { return _stsNewRcpBlink; }
            private set { SetProperty(ref _stsNewRcpBlink, value); }
        }

        private bool _hasAnySpliceAlarm;
        public bool HasAnySpliceAlarm
        {
            get { return _hasAnySpliceAlarm; }
            private set
            {
                if (SetProperty(ref _hasAnySpliceAlarm, value))
                {
                    OnPropertyChanged(nameof(BannerText));
                }
            }
        }

        public string BannerText
        {
            get
            {
                if (!HasAnySpliceAlarm) return "妫€娴嬫甯?| SYSTEM NORMAL";
                var parts = new List<string>();
                if (SpliceValue1OutOfRange) parts.Add("P-01 前段");
                if (SpliceValue2OutOfRange) parts.Add("P-02 中段");
                if (SpliceValue3OutOfRange) parts.Add("P-03 后段");
                return "报警 | ALARM: " + string.Join("、", parts.ToArray()) + " 搭接量超出标准范围 (4±2mm)";
            }
        }

        public ICommand ShowMonitorCommand { get; }
        public ICommand ShowOverlapCommand { get; }
        public ICommand ShowAlarmsCommand { get; }
        public ICommand ShowHistoryCommand { get; }
        public ICommand ExitCommand { get; }
        public string SystemStatus { get { return "主线运行中"; } }
        public string FooterStatus { get { return "璁惧鍦ㄧ嚎 路 PLC 杩炴帴姝ｅ父"; } }
        public string Version { get { return "v1.0.0"; } }

        public bool OpcConnected { get; private set; }

        private void SelectPage(HmiPage page, object viewModel)
        {
            SelectedPage = page;
            CurrentPage = viewModel;
        }

        private OperateResult<OPCUADevice> GetDeviceByPath(string xmlPath)
        {
            List<Project> projects = new ConfigManage().LoadProjects(xmlPath);

            if (projects != null && projects.Count > 0)
            {
                Project project = projects[0];

                if (project.OPCUAList.Count > 0)
                {
                    var device = project.OPCUAList[0];

                    if (device.IsActive)
                    {
                        return OperateResult.CreateSuccessResult(device);
                    }
                    else
                    {
                        return OperateResult.CreateFailResult<OPCUADevice>("请检查配置是否激活");
                    }
                }
                else
                {
                    return OperateResult.CreateFailResult<OPCUADevice>("请检查配置是否有西门子设备");
                }
            }
            else
            {
                return OperateResult.CreateFailResult<OPCUADevice>("请检查配置文件是否正确");
            }
        }


        int currentHeartbeatValue = 1;
        private string xmlPath = System.AppDomain.CurrentDomain.BaseDirectory + "\\Settings\\settings.json";
        private DispatcherTimer updateTimer;
        private int heartbeatFailureCount = 0; // 心跳失败次数计数器
        private const int MaxHeartbeatFailures = 200000; // 鍏佽鐨勬渶澶уけ璐ユ鏁?
        private int previousHeartbeatValue = -1; // 存储上一次的心跳值
        private int heartbeatCheckInterval = 1000;
        private bool firstConnect;//绗竴娆¤繛鎺ユ爣蹇椾负

        private void PlcDevice_AlarmTriggerEvent(object sender, AlarmEventArgs e)
        {
            var vb = sender as OPCUAVariable;
            if (vb == null) return;

            
            string message = vb.HighAlarmEnable ? vb.HighAlarmNote : vb.LowAlarmNote;

        //    Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        if (e.IsTrigger)
        //        {
        //            var exists = AlarmList.FirstOrDefault(a => a.Message == message && a.Status == position);
        //            if (exists == null)
        //            {
        //                AlarmList.Insert(0, new AlarmItemModel
        //                {
        //                    Time = DateTime.Now.ToString(),
        //                    Status = position,
        //                    Message = message
        //                });
        //            }
        //        }
        //        else
        //        {
        //            var toRemove = AlarmList.Where(a => a.Message == message && a.Status == position).ToList();
        //            foreach (var item in toRemove)
        //            {
        //                AlarmList.Remove(item);
        //            }
        //        }
        //    });
        }
        private async Task PLCCOM(OPCUADevice device, OPCUA ua)
        {
            while (!device.Cts.IsCancellationRequested)
            {
                if (device.IsConnected)
                {
                    currentHeartbeatValue = ReadHeartbeat();
                }

                if (firstConnect)
                {
                    await AttemptConnectionAndInitialData(ua, device);
                    firstConnect = false;
                }
                else
                {
                    if (currentHeartbeatValue == previousHeartbeatValue)
                    {
                        heartbeatFailureCount++;
                        if (heartbeatFailureCount >= MaxHeartbeatFailures)
                        {
                            // 杩炵画澶氭澶辫触锛屽彲鑳藉凡鏂紑杩炴帴
                            device.IsConnected = false;
                            await AttemptConnectionAndInitialData(ua, device);
                            heartbeatFailureCount = 0; // 閲嶇疆璁℃暟鍣?

                        }
                    }
                    else
                    {
                        // 蹇冭烦鍊煎彉鍖栵紝閲嶇疆澶辫触璁℃暟鍣?
                        heartbeatFailureCount = 0;
                        previousHeartbeatValue = currentHeartbeatValue;
                    }
                }
                await Task.Delay(heartbeatCheckInterval); // 妫€鏌ラ棿闅?
            }
        }

        private int ReadHeartbeat()
        {
            if (CommonMethods.plcDevice.CurrentValue.ContainsKey("Heartbeat") &&
                  CommonMethods.plcDevice.CurrentValue["Heartbeat"] != null &&
                  int.TryParse(CommonMethods.plcDevice.CurrentValue["Heartbeat"].ToString(), out int result))
            {
                return result;
            }
            else
            {
                return 1;
            }
        }

        private async Task AttemptConnectionAndInitialData(OPCUA item, OPCUADevice device, int delayMilliseconds = 2000)
        {
            try
            {
                item.OpcUaName = "prick";

                await item.ConnectServer(device.ServerUrl);

                device.IsConnected = true;
                OpcConnected = true;
                Task.Run(async () =>
                {
                    await GetOPCUAValue(device, item, CommonMethods.plcDevice.Cts.Token);
                }, CommonMethods.plcDevice.Cts.Token);
            }
            catch (Exception ex)
            {
                device.IsConnected = false;
                OpcConnected = false;
                NLogHelper.Warn("OPCUA寤虹珛閫氳澶辫触", ex);
            }

            await Task.CompletedTask;
        }

        private async Task GetOPCUAValue(OPCUADevice opcua, OPCUA ua, CancellationToken token)
        {
          
            while (!opcua.Cts.IsCancellationRequested && opcua.IsConnected && !token.IsCancellationRequested)
            {
                foreach (var gp in opcua.GroupList)
                {
                    if (token.IsCancellationRequested || !opcua.IsConnected)
                    {
                        break;
                    }

                    if (gp == null || gp.VarList == null || gp.VarList.Count == 0)
                    {
                        continue;
                    }

                    try
                    {
                        List<NodeId> tagList = new List<NodeId>();
                        foreach (var variable in gp.VarList)
                        {
                            tagList.Add(new NodeId(variable.VarAddress));
                        }
                        List<DataValue> result = ua.ReadNodes(tagList.ToArray());
                        if (result != null)
                        {
                            for (int i = 0; i < tagList.Count && i < result.Count; i++)
                            {
                                var variable = gp.VarList[i];
                                variable.VarValue = result[i].Value;
                                variable.VarValue = MigrationLib.GetMigrationValue(variable.VarValue, variable.Scale.ToString(), variable.Offset.ToString()).Content;
                                lock (ValueLock)
                                {
                                    opcua.Update(variable);
                                }
                            }
                        }
                    }
                    catch (Exception EX)
                    {
                        NLogHelper.Warn($"璇诲彇plc鏁版嵁澶辫触锛屾暟鎹粍: {gp.GroupName}", EX);
                    }
                }

                await Task.Delay(250, token); // 鏁磋疆鎵弿鍚庣殑缁熶竴鑺傛媿
            }
        }
    }
}
