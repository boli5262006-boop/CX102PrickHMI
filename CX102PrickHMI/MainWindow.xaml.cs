using CX102PrickHMI.ViewModels;
using Microsoft.Extensions.DependencyInjection; // 添加此行以支持 GetService 扩展方法
using System.Windows;

namespace CX102PrickHMI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = ((App)Application.Current).Services.GetService<MainViewModel>();
        }
    }
}
