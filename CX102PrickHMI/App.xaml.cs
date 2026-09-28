using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using HslControls;

namespace CX102PrickHMI
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public IServiceProvider Services { get; private set; }
        public App()
        {
            // 注册控件示例，如果注册失败，你的控件仍然只能使用8个小时
            if (HslControls.Authorization.SetAuthorizationCode("7675a4d1-eeac-4e7f-bd92-acc7b58570ec"))
            {
                // 注册成功 Registration Successful;
               // System.Windows.Forms.MessageBox.Show("Registration Successful", "Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // 注册失败 Registration Failed;
                System.Windows.Forms.MessageBox.Show("Registration Failed", "Check", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Services = Configure.ConfigureService.Load();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // [TEMP DIAGNOSTIC] WPF 绑定错误落盘，定位搭接页徽章触发器问题，验证后删除
            var bindingLog = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "logs", "binding-errors.log");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(bindingLog));
            var listener = new System.Diagnostics.TextWriterTraceListener(bindingLog, "bindingTrace");
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Warning;
            base.OnStartup(e);
        }
    }
}
