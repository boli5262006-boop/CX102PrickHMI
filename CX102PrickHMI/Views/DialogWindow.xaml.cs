using System.Windows;
using System.Windows.Input;

namespace CX102PrickHMI.Views
{
    public partial class DialogWindow : HandyControl.Controls.Window
    {
        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
            nameof(Message), typeof(string), typeof(DialogWindow), new PropertyMetadata(string.Empty));

        public DialogWindow()
        {
            InitializeComponent();
        }

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
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

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
