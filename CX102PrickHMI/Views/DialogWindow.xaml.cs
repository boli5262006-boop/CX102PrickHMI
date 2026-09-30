using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CX102PrickHMI.Views
{
    public partial class DialogWindow : HandyControl.Controls.Window
    {
        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
            nameof(Message), typeof(string), typeof(DialogWindow), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty AlarmModeProperty = DependencyProperty.Register(
            nameof(AlarmMode), typeof(bool), typeof(DialogWindow), new PropertyMetadata(false, OnAlarmModeChanged));

        private bool _allowClose;

        public DialogWindow()
        {
            InitializeComponent();
        }

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public bool AlarmMode
        {
            get { return (bool)GetValue(AlarmModeProperty); }
            set { SetValue(AlarmModeProperty, value); }
        }

        private static void OnAlarmModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (DialogWindow)d;
            var alarm = (bool)e.NewValue;
            w.OkButton.Visibility = alarm ? Visibility.Collapsed : Visibility.Visible;
            if (alarm)
            {
                w.CardBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                w.CardBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0xEF, 0x44, 0x44),
                    BlurRadius = 24,
                    Opacity = 0.7,
                    ShadowDepth = 0
                };
                w.StartAlarmPulse();
            }
        }

        private void StartAlarmPulse()
        {
            var storyboard = (Storyboard)FindResource("AlarmBorderPulse");
            if (IsLoaded)
            {
                storyboard.Begin(CardBorder);
            }
            else
            {
                Loaded += (s, e) => storyboard.Begin(CardBorder);
            }
        }

        public static void ShowInfo(string message, string title = "提示")
        {
            var window = new DialogWindow { Message = message, Title = title };
            var owner = Application.Current?.MainWindow;
            if (owner != null && owner.IsLoaded)
            {
                window.Owner = owner;
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            window.ShowDialog();
        }

        // 通讯异常：无确定按钮、Esc 与手动关闭均被拦截、非模态置顶；由调用方在通讯恢复后 CloseAlarm()
        public static DialogWindow ShowAlarm(string message, string title = "PLC 通讯异常")
        {
            var window = new DialogWindow { Message = message, Title = title, AlarmMode = true, Topmost = true };
            var owner = Application.Current?.MainWindow;
            if (owner != null && owner.IsLoaded)
            {
                window.Owner = owner;
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            window.Show();
            return window;
        }

        // 通讯恢复后由调用方调用：解除关闭保护并关闭窗口
        public void CloseAlarm()
        {
            _allowClose = true;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (AlarmMode)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (AlarmMode && !_allowClose)
            {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }
    }
}