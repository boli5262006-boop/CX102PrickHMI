using System.Collections.ObjectModel;
using CX102PrickHMI.Models;

namespace CX102PrickHMI.ViewModels
{
    public sealed class AlarmListViewModel
    {
        public AlarmListViewModel()
        {
            Items = new ObservableCollection<AlarmEntry>
            {
                new AlarmEntry("2026-07-29 08:23:15", "伺服1 回原超时", "警告", string.Empty),
                new AlarmEntry("2026-07-29 09:10:42", "刺针环1 实际位置偏差过大", "错误", string.Empty),
                new AlarmEntry("2026-07-29 10:05:03", "伺服2 编码器通讯异常", "错误", string.Empty),
                new AlarmEntry("2026-07-29 11:18:27", "气压不足，请检查气源", "警告", string.Empty),
                new AlarmEntry("2026-07-29 13:44:56", "刺针环2 配方值已更新", "信息", string.Empty),
                new AlarmEntry("2026-07-29 14:02:11", "系统启动完成", "信息", string.Empty)
            };
        }

        public ObservableCollection<AlarmEntry> Items { get; }
    }
}
