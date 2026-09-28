using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CX102PrickHMI.Models;

namespace CX102PrickHMI.ViewModels
{
    public sealed class MonitorViewModel : ObservableObject
    {
        private string _calibrationMessage = "模拟数据 · 未连接设备";
        private string _servo1Actual = "0.00";
        private string _servo2Actual = "0.00";
        private readonly DispatcherTimer _simulationTimer;
        private int _simulationStepIndex = 3;

        public MonitorViewModel()
        {
            CommonMethods.plc.UseSecurity = false;
            Steps = new ObservableCollection<ProcessStep>
            {
                new ProcessStep("主线停机", "已完成", ProcessStepState.Completed, 1),
                new ProcessStep("配方加载", "已完成", ProcessStepState.Completed, 2),
                new ProcessStep("离合松开", "已完成", ProcessStepState.Completed, 3),
                new ProcessStep("槽辊打开", "异常", ProcessStepState.Error, 4),
                new ProcessStep("夹紧到位", "未开始", ProcessStepState.Pending, 5),
                new ProcessStep("位置到达", "未开始", ProcessStepState.Pending, 6),
                new ProcessStep("槽辊收回", "未开始", ProcessStepState.Pending, 7),
                new ProcessStep("离合夹紧", "未开始", ProcessStepState.Pending, 8),
                new ProcessStep("完成", "未开始", ProcessStepState.Pending, 9)
            };

            CalibrateCommand = new RelayCommand(() => CalibrationMessage = "校准请求已发送");
            SettingsCommand = new RelayCommand(() => CalibrationMessage = "模拟设置面板");
            MaintenanceCommand = new RelayCommand(() => CalibrationMessage = "维修模式未启用");

            _simulationTimer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(2)
            };
            _simulationTimer.Tick += OnSimulationTimerTick;
            _simulationTimer.Start();
        }

        public ObservableCollection<ProcessStep> Steps { get; }
        public bool IsAlarmActive
        {
            get
            {
                return Steps.Any(step => step.State == ProcessStepState.Error);
            }
        }
        public string RecipeRing1 { get { return "125.00"; } }
        public string RecipeRing2 { get { return "198.50"; } }
        public string ActualRing1 { get { return "124.98"; } }
        public string ActualRing2 { get { return "198.52"; } }
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
        public string CalibrationMessage { get { return _calibrationMessage; } private set { SetProperty(ref _calibrationMessage, value); } }
        public ICommand CalibrateCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand MaintenanceCommand { get; }

        private void OnSimulationTimerTick(object sender, System.EventArgs e)
        {
            AdvanceSimulation();
        }

        private void AdvanceSimulation()
        {
            _simulationStepIndex++;
            if (_simulationStepIndex >= Steps.Count)
            {
                _simulationStepIndex = 3;
            }

            for (var index = 0; index < Steps.Count; index++)
            {
                var step = Steps[index];
                if (index < _simulationStepIndex)
                {
                    step.UpdateState(ProcessStepState.Completed, "已完成");
                }
                else if (index == _simulationStepIndex && index == 3)
                {
                    step.UpdateState(ProcessStepState.Error, "异常");
                }
                else if (index == _simulationStepIndex)
                {
                    step.UpdateState(ProcessStepState.Pending, "进行中");
                }
                else
                {
                    step.UpdateState(ProcessStepState.Pending, "未开始");
                }
            }

            OnPropertyChanged(nameof(IsAlarmActive));
        }
    }
}
