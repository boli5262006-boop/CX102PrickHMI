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

    // 搭接三段判定：单一分类结果，Warning/OutOfRange 两个布尔均由它投影而来
    internal enum SpliceBand
    {
        Normal,
        Warning,
        Alarm
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
            //    AlarmState="到达",
            //    AlarmNote = "Test",
            //    VarName = "limitswitch"
            //});

            Steps = new ObservableCollection<ProcessStep>();
            AlarmItems = new ObservableCollection<AlarmEntry>();
            HistoryItems = new ObservableCollection<AlarmEntry>();

            // 页面视图为容器单例，切换时直接复用已构建的视觉树，避免重复实例化造成卡顿
            _monitorPage = monitorView;
            _overlapPage = overlapView;
            _alarmsPage = alarmListView;
            _historyPage = historyView;

            // 四个页面的 DataContext 统一指向 MainViewModel
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
            HoldJogFwdCommand = new RelayCommand<bool>(value => _ = SendMomentaryAsync("CmdJogFwd", value));
            HoldJogBwdCommand = new RelayCommand<bool>(value => _ = SendMomentaryAsync("CmdJogBwd", value));
            ResetCommand = new AsyncRelayCommand(ResetPulseAsync);

            // 历史查询：默认前一日 00:00:00 至 当日 00:00:00（24 小时，满足 48 小时规则）
            var today = DateTime.Today;
            HistoryStartTime = today.AddDays(-1);
            HistoryEndTime = today;
            QueryHistoryCommand = new AsyncRelayCommand(QueryHistoryAsync);
            QuickRange2hCommand = new AsyncRelayCommand(QueryLast2HoursAsync);
            QuickRange6hCommand = new AsyncRelayCommand(QueryLast6HoursAsync);

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

        // CurrentValue 读写互斥锁：后台轮询写 / UI 泵读共用
        private static readonly object ValueLock = new object();

        // 500ms 快照泵：只读 CurrentValue 刷新界面属性，绝不写 PLC
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

            UpdateSpliceSnapshot(values, "SpliceValue1", v => SpliceValue1Value = v, v => SpliceValue1Delta = v, v => SpliceValue1Warning = v, v => SpliceValue1OutOfRange = v);
            UpdateSpliceSnapshot(values, "SpliceValue2", v => SpliceValue2Value = v, v => SpliceValue2Delta = v, v => SpliceValue2Warning = v, v => SpliceValue2OutOfRange = v);
            UpdateSpliceSnapshot(values, "SpliceValue3", v => SpliceValue3Value = v, v => SpliceValue3Delta = v, v => SpliceValue3Warning = v, v => SpliceValue3OutOfRange = v);
            UpdateBoolSnapshot(values, "StsNewRcpBlink", v => StsNewRcpBlink = v);
            RefreshSpliceBanner();


            // 原点值输入框：对应轴键盘打开期间暂停刷新，避免编辑值被覆盖
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
                return; // 缺键/非法值保留上次值
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
                return; // 缺键/非法值保留上次值
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

        // 搭接三段分类：严格不等号、无容差。v<2 或 v>6 报警；2/6 精确值为预警；3..5（含端点）正常
        private static SpliceBand ClassifySplice(double v)
        {
            if (v < 2.0 || v > 6.0) return SpliceBand.Alarm;    // 超出 4±2，端点不判红
            if (v < 3.0 || v > 5.0) return SpliceBand.Warning;  // 超出 4±1 但仍在 4±2 内，端点不判黄
            return SpliceBand.Normal;                           // 3..5 含端点
        }

        private void UpdateSpliceSnapshot(Dictionary<string, object> values, string key, Action<double> assignValue, Action<string> assignDelta, Action<bool> assignWarning, Action<bool> assignOutOfRange)
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
            var band = ClassifySplice(v);
            assignWarning(band == SpliceBand.Warning);
            assignOutOfRange(band == SpliceBand.Alarm);
            var suffix = band == SpliceBand.Alarm ? " 超限" : band == SpliceBand.Warning ? " 预警" : "";
            assignDelta((v - 4.0).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + suffix);
        }

        private void RefreshSpliceBanner()
        {
            var anyWarning = SpliceValue1Warning || SpliceValue2Warning || SpliceValue3Warning;
            HasAnySpliceAlarm = SpliceValue1OutOfRange || SpliceValue2OutOfRange || SpliceValue3OutOfRange;
            HasAnySpliceWarning = anyWarning && !HasAnySpliceAlarm;
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

        // —— 监控页（PLC 绑定属性） ——
        public ObservableCollection<ProcessStep> Steps { get; private set; }

        // 读值快照属性：由 UpdateTimer_Tick 从 CurrentValue 刷新
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

        // 语义：false = 运行中，true = 已停止
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

        // 原点输入框校验失败标记（红色边框）
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

        // 报警脉冲本轮恒为关（自 AlarmListViewModel 合并处，本轮用不到）
        public bool IsAlarmActive
        {
            get { return false; }
        }

        // —— 原点数字键盘状态 ——
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

        // 首键替换标志：打开键盘或 C 清空后置 true，下一个数字键整体替换输入缓冲
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

        // —— 键盘 / 点动命令 ——
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
        public IRelayCommand<bool> HoldJogFwdCommand { get; }
        public IRelayCommand<bool> HoldJogBwdCommand { get; }
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
                    KeypadErrorText = "原点值必须大于0";
                    SetKeypadInvalid(true);
                    return;
                }

                value = (float)Math.Round((double)value, 2);

                var varName = KeypadTarget == KeypadTarget.In ? "HomingRefPosIn" : "HomingRefPosOut";
                string address;
                if (!PlcTagLookup.TryGetAddress(CommonMethods.plcDevice, varName, out address))
                {
                    CalibrationMessage = "未找到点位地址";
                    CloseKeypad();
                    return;
                }

                if (!await CommonMethods.plc.WriteNodeAsync<float>(address, value))
                {
                    // 写入失败：键盘保持打开，允许重试
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

        // 点动命令：按下发 true / 松开或失去捕获发 false
        private async Task SendMomentaryAsync(string varName, bool value)
        {
            try
            {
                string address;
                if (!PlcTagLookup.TryGetAddress(CommonMethods.plcDevice, varName, out address))
                {
                    CalibrationMessage = "未找到点位地址: " + varName;
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

        // 复位：一次点击发 true，50ms 后发 false
        private async Task ResetPulseAsync()
        {
            try
            {
                string address;
                if (!PlcTagLookup.TryGetAddress(CommonMethods.plcDevice, "CmdReset", out address))
                {
                    CalibrationMessage = "未找到点位地址: CmdReset";
                    return;
                }
                if (!await CommonMethods.plc.WriteNodeAsync<bool>(address, true))
                {
                    CalibrationMessage = "复位写入失败";
                    return;
                }
                await Task.Delay(100);
                if (!await CommonMethods.plc.WriteNodeAsync<bool>(address, false))
                {
                    CalibrationMessage = "复位写入失败";
                }
            }
            catch (Exception ex)
            {
                CalibrationMessage = "复位写入失败";
                NLogHelper.Warn("复位命令写入失败", ex);
            }
        }

        // —— 报警记录页（原 AlarmListViewModel 合并） ——
        public ObservableCollection<AlarmEntry> AlarmItems { get; private set; }

        // —— 历史报警页（原 HistoryAlarmViewModel 合并） ——
        public ObservableCollection<AlarmEntry> HistoryItems { get; private set; }

        // 查询成功后的结果摘要：共 N 条记录 · {start} 至 {end}；失败时保留上次成功摘要
        private string _dateSummary = "";
        public string DateSummary
        {
            get { return _dateSummary; }
            private set { SetProperty(ref _dateSummary, value); }
        }

        // 历史查询精确起止时间（DateTimePicker SelectedDateTime 绑定，可空）
        private DateTime? _historyStartTime;
        public DateTime? HistoryStartTime
        {
            get { return _historyStartTime; }
            set { SetProperty(ref _historyStartTime, value); }
        }

        private DateTime? _historyEndTime;
        public DateTime? HistoryEndTime
        {
            get { return _historyEndTime; }
            set { SetProperty(ref _historyEndTime, value); }
        }

        // 内联查询/DB 失败文本；校验拒绝走对话框，不占用此字段
        private string _historyStatus = "";
        public string HistoryStatus
        {
            get { return _historyStatus; }
            private set { SetProperty(ref _historyStatus, value); }
        }

        // AsyncRelayCommand 运行期间自动禁用，防止快速连点并发查询
        public IRelayCommand QueryHistoryCommand { get; }
        public IRelayCommand QuickRange2hCommand { get; }
        public IRelayCommand QuickRange6hCommand { get; }

        // 跨命令并发护栏：任一历史查询在途时，其余查询/快捷命令静默跳过
        private bool _historyQueryRunning;

        // 历史查询：校验（精确 48 小时上限）→ Task.Run 查库至当前时刻（便于配对跨窗口的离开）→ 内存配对投影 → UI 线程回填
        private async Task QueryHistoryAsync()
        {
            if (_historyQueryRunning)
            {
                return;
            }
            _historyQueryRunning = true;
            try
            {
                if (HistoryStartTime == null || HistoryEndTime == null)
                {
                    RejectHistoryRange("请选择有效的起始与结束时间");
                    return;
                }

                var start = HistoryStartTime.Value;
                var end = HistoryEndTime.Value;

                if (start > end)
                {
                    RejectHistoryRange("起始时间不能晚于结束时间，请检查所选范围");
                    return;
                }

                // 精确 48 小时上限：恰好 48 小时允许，超过拒绝
                if (end - start > TimeSpan.FromHours(48))
                {
                    RejectHistoryRange("查询跨度不能超过 48 小时，请缩小时间范围");
                    return;
                }

                try
                {
                    // 实际查库到当前时刻：结束之后发生的离开也能配对出真实恢复时间
                    var rows = await Task.Run(() => alarmservice.GetByTimeRange(start, DateTime.Now));
                    var entries = BuildHistoryProjection(rows, start, end);

                    var dispatcher = Application.Current != null ? Application.Current.Dispatcher : null;
                    if (dispatcher == null)
                    {
                        return;
                    }

                    dispatcher.Invoke(new Action(() =>
                    {
                        HistoryItems.Clear();
                        foreach (var entry in entries)
                        {
                            HistoryItems.Add(entry);
                        }
                    }));

                    HistoryStatus = "";
                    DateSummary = string.Format(
                        CultureInfo.InvariantCulture,
                        "共 {0} 条记录 · {1} 至 {2}",
                        entries.Count, FormatHistoryTimestamp(start), FormatHistoryTimestamp(end));
                }
                catch (Exception ex)
                {
                    HistoryStatus = "查询失败，请稍后重试";
                    NLogHelper.Warn("历史报警查询失败", ex);
                    Views.DialogWindow.ShowInfo("查询失败，请稍后重试");
                }
            }
            finally
            {
                _historyQueryRunning = false;
            }
        }

        // 快捷范围：单次取时保证 start <= end，设置控件后立即查询
        private async Task QueryQuickRangeAsync(double hours)
        {
            var now = DateTime.Now;
            HistoryStartTime = now.AddHours(-hours);
            HistoryEndTime = now;
            await QueryHistoryAsync();
        }

        private async Task QueryLast2HoursAsync()
        {
            await QueryQuickRangeAsync(2);
        }

        private async Task QueryLast6HoursAsync()
        {
            await QueryQuickRangeAsync(6);
        }

        // 校验拒绝：主题化对话框 + 日志，不执行查询，不改动当前结果/摘要/状态
        private void RejectHistoryRange(string reason)
        {
            NLogHelper.Warn("历史查询时间范围被拒绝: " + reason);
            Views.DialogWindow.ShowInfo(reason);
        }

        // 内存配对投影（设计 D5），不改写任何事件行：按查询顺序（时间升序，同刻到达先于离开）遍历，
        // 身份键为归一化 (VarName, AlarmNote)；到达压栈、离开弹出最近同身份到达配成一集；
        // 孤儿离开（到达在窗口前）丢弃；未配对到达（仍激活）恢复时间显示空串
        private static List<AlarmEntry> BuildHistoryProjection(List<Models.Alarms> rows, DateTime windowStartInclusive, DateTime windowEndExclusive)
        {
            var arrivals = new List<Models.Alarms>();
            var openArrivals = new Dictionary<Tuple<string, string>, Stack<Models.Alarms>>();
            var recoveryByArrival = new Dictionary<Models.Alarms, DateTime>();

            foreach (var row in rows)
            {
                if (row == null)
                {
                    continue;
                }

                var state = NormalizeIdentity(row.AlarmState);
                if (state == "到达")
                {
                    var key = Tuple.Create(NormalizeIdentity(row.VarName), NormalizeIdentity(row.AlarmNote));
                    Stack<Models.Alarms> stack;
                    if (!openArrivals.TryGetValue(key, out stack))
                    {
                        stack = new Stack<Models.Alarms>();
                        openArrivals[key] = stack;
                    }
                    stack.Push(row);
                    arrivals.Add(row);
                }
                else if (state == "离开")
                {
                    var key = Tuple.Create(NormalizeIdentity(row.VarName), NormalizeIdentity(row.AlarmNote));
                    Stack<Models.Alarms> stack;
                    if (openArrivals.TryGetValue(key, out stack) && stack.Count > 0 && row.InsertTime.HasValue)
                    {
                        recoveryByArrival[stack.Pop()] = row.InsertTime.Value;
                    }
                    // 孤儿离开（到达不在窗口/无未闭合到达）或离开无时间戳：丢弃，保持到达未配对
                }
            }

            // 只投影到达时间落在请求窗口内的行（查库至 now 带回的行均 >= 起始日，此处仍显式过滤）
            for (int i = arrivals.Count - 1; i >= 0; i--)
            {
                var alarmTime = arrivals[i].InsertTime;
                if (!alarmTime.HasValue || alarmTime.Value < windowStartInclusive || alarmTime.Value >= windowEndExclusive)
                {
                    arrivals.RemoveAt(i);
                }
            }

            // 最新到达在前
            arrivals.Sort((a, b) => b.InsertTime.Value.CompareTo(a.InsertTime.Value));

            var projected = new List<AlarmEntry>();
            foreach (var arrival in arrivals)
            {
                DateTime leaveTime;
                var recovery = recoveryByArrival.TryGetValue(arrival, out leaveTime)
                    ? FormatHistoryTimestamp(leaveTime)
                    : string.Empty;

                projected.Add(new AlarmEntry(
                    FormatHistoryTimestamp(arrival.InsertTime.Value),
                    arrival.AlarmNote ?? string.Empty,
                    "到达",
                    recovery,
                    arrival.VarName));
            }

            return projected;
        }

        private static string NormalizeIdentity(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }

        private static string FormatHistoryTimestamp(DateTime time)
        {
            return time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        // —— 搭接监控页 (OverlapMonitorView) ——
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

        private bool _spliceValue1Warning;
        public bool SpliceValue1Warning
        {
            get { return _spliceValue1Warning; }
            private set { SetProperty(ref _spliceValue1Warning, value); }
        }

        private bool _spliceValue2Warning;
        public bool SpliceValue2Warning
        {
            get { return _spliceValue2Warning; }
            private set { SetProperty(ref _spliceValue2Warning, value); }
        }

        private bool _spliceValue3Warning;
        public bool SpliceValue3Warning
        {
            get { return _spliceValue3Warning; }
            private set { SetProperty(ref _spliceValue3Warning, value); }
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

        // 预警聚合：任一通道预警且无通道报警（红色优先已在 ViewModel 内折叠）
        private bool _hasAnySpliceWarning;
        public bool HasAnySpliceWarning
        {
            get { return _hasAnySpliceWarning; }
            private set
            {
                if (SetProperty(ref _hasAnySpliceWarning, value))
                {
                    OnPropertyChanged(nameof(BannerText));
                }
            }
        }

        public string BannerText
        {
            get
            {
                if (HasAnySpliceAlarm)
                {
                    var parts = new List<string>();
                    if (SpliceValue1OutOfRange) parts.Add("P-01 前段");
                    if (SpliceValue2OutOfRange) parts.Add("P-02 中段");
                    if (SpliceValue3OutOfRange) parts.Add("P-03 后段");
                    return "报警 | ALARM: " + string.Join("、", parts.ToArray()) + " 搭接量超出标准范围 (4±2mm)";
                }
                if (HasAnySpliceWarning)
                {
                    var parts = new List<string>();
                    if (SpliceValue1Warning) parts.Add("P-01 前段");
                    if (SpliceValue2Warning) parts.Add("P-02 中段");
                    if (SpliceValue3Warning) parts.Add("P-03 后段");
                    return "预警 | WARNING: " + string.Join("、", parts.ToArray()) + " 搭接量接近标准范围 (4±1mm)";
                }
                return "检测正常 | SYSTEM NORMAL";
            }
        }

        public ICommand ShowMonitorCommand { get; }
        public ICommand ShowOverlapCommand { get; }
        public ICommand ShowAlarmsCommand { get; }
        public ICommand ShowHistoryCommand { get; }
        public ICommand ExitCommand { get; }
        public string SystemStatus { get { return "主线运行中"; } }
        public string FooterStatus { get { return "设备在线 · PLC 连接正常"; } }
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
        private const int MaxHeartbeatFailures = 200000; // 允许的最大失败次数
        private int previousHeartbeatValue = -1; // 存储上一次的心跳值
        private int heartbeatCheckInterval = 1000;
        private bool firstConnect;//第一次连接标志为

        // 报警事件：轮询线程触发。先同步落库一条 Alarms，再调度到 UI 线程维护实时报警列表
        private void PlcDevice_AlarmTriggerEvent(object sender, AlarmEventArgs e)
        {
            var variable = sender as VariableBase;
            if (variable == null || e == null)
            {
                return;
            }

            // 只处理已启用的报警方向（上限/下限）
            bool isMaxDirection = variable.HighAlarmEnable;
            bool isMinDirection = variable.LowAlarmEnable;
            if (!isMaxDirection && !isMinDirection)
            {
                return;
            }

            // 描述取已启用方向的备注；未配置时借用另一方向，再兜底事件描述/变量名
            string alarmNote = isMaxDirection ? variable.HighAlarmNote : variable.LowAlarmNote;
            if (string.IsNullOrWhiteSpace(alarmNote))
            {
                alarmNote = isMaxDirection ? variable.LowAlarmNote : variable.HighAlarmNote;
            }
            if (string.IsNullOrWhiteSpace(alarmNote))
            {
                alarmNote = e.AlarmNote;
            }
            if (string.IsNullOrWhiteSpace(alarmNote))
            {
                alarmNote = variable.VarName;
            }
            if (string.IsNullOrWhiteSpace(alarmNote))
            {
                alarmNote = "未命名报警";
            }

            string varName = variable.VarName;
            bool isArrival = e.IsTrigger;
            string alarmState = isArrival ? "到达" : "离开";
            DateTime insertTime = DateTime.Now;

            // 每次事件只落库一条记录，失败只记日志，不影响界面刷新
            try
            {
                if (alarmservice != null)
                {
                    alarmservice.Insert(new Models.Alarms()
                    {
                        InsertTime = insertTime,
                        Symbol = null,
                        AlarmState = alarmState,
                        AlarmNote = alarmNote,
                        VarName = varName
                    });
                }
            }
            catch (Exception ex)
            {
                NLogHelper.Warn("报警记录写入数据库失败: " + alarmNote, ex);
            }

            // 实时列表的所有增删都在 UI 线程执行
            var dispatcher = Application.Current != null ? Application.Current.Dispatcher : null;
            if (dispatcher == null)
            {
                return;
            }

            dispatcher.BeginInvoke(new Action(() =>
            {
                if (isArrival)
                {
                    // 到达：按 变量名+内容 去重后插入队首
                    foreach (var item in AlarmItems)
                    {
                        if (item.Message == alarmNote && string.Equals(item.VarName, varName, StringComparison.Ordinal))
                        {
                            return;
                        }
                    }
                    AlarmItems.Insert(0, new AlarmEntry(
                        insertTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        alarmNote,
                        alarmState,
                        string.Empty,
                        varName));
                }
                else
                {
                    // 离开：移除同 变量名+内容 的在档行
                    for (int i = AlarmItems.Count - 1; i >= 0; i--)
                    {
                        var item = AlarmItems[i];
                        if (item.Message == alarmNote && string.Equals(item.VarName, varName, StringComparison.Ordinal))
                        {
                            AlarmItems.RemoveAt(i);
                        }
                    }
                }
            }));
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
                            // 连续多次失败，可能已断开连接
                            device.IsConnected = false;
                            await AttemptConnectionAndInitialData(ua, device);
                            heartbeatFailureCount = 0; // 重置计数器

                        }
                    }
                    else
                    {
                        // 心跳值变化，重置失败计数器
                        heartbeatFailureCount = 0;
                        previousHeartbeatValue = currentHeartbeatValue;
                    }
                }
                await Task.Delay(heartbeatCheckInterval); // 检查间隔
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
                NLogHelper.Warn("OPCUA建立通讯失败", ex);
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
                        NLogHelper.Warn($"读取plc数据失败，数据组: {gp.GroupName}", EX);
                    }
                }

                await Task.Delay(250, token); // 整轮扫描后的统一节拍
            }
        }
    }
}
