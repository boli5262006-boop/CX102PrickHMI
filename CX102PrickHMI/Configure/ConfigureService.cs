using CX102PrickHMI.Interfaces;
using CX102PrickHMI.Services;
using CX102PrickHMI.ViewModels;
using CX102PrickHMI.Views;
using Microsoft.Extensions.DependencyInjection;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CX102PrickHMI.Configure
{
    internal class ConfigureService
    {
        public static IServiceProvider Load()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IAlarmRepository, AlarmService>();

            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainViewModel>();

            services.AddSingleton<MonitorView>();
            services.AddSingleton<OverlapMonitorView>();
            services.AddSingleton<AlarmListView>();
            services.AddSingleton<HistoryAlarmView>();

            return services.BuildServiceProvider();
        }
    }
}
