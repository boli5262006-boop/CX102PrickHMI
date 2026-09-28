using System.Collections.ObjectModel;
using CX102PrickHMI.Models;

namespace CX102PrickHMI.ViewModels
{
    public sealed class HistoryAlarmViewModel
    {
        public HistoryAlarmViewModel()
        {
            Items = new ObservableCollection<AlarmEntry>
            {
                new AlarmEntry("2026-07-28 02:14:33", "伺服1 过载保护触发", "错误", "2026-07-28 02:31:08"),
                new AlarmEntry("2026-07-27 15:46:20", "刺针环2 位置传感器信号丢失", "错误", "2026-07-27 16:02:45"),
                new AlarmEntry("2026-07-27 09:22:10", "冷却液温度偏高", "警告", "2026-07-27 09:38:52"),
                new AlarmEntry("2026-07-26 21:05:47", "伺服2 回原超时", "警告", "2026-07-26 21:19:33"),
                new AlarmEntry("2026-07-26 14:33:18", "刺针环1 实际位置偏差过大", "错误", "2026-07-26 14:50:01"),
                new AlarmEntry("2026-07-25 18:12:55", "气压不足，请检查气源", "警告", "2026-07-25 18:27:40"),
                new AlarmEntry("2026-07-25 08:40:12", "配方参数已更新", "信息", "—"),
                new AlarmEntry("2026-07-24 22:18:39", "系统紧急停止按钮按下", "错误", "2026-07-24 22:25:17")
            };
        }

        public ObservableCollection<AlarmEntry> Items { get; }
        public string DateSummary { get { return "共 8 条历史记录 · 2026-07-01 至 2026-07-29"; } }
    }
}
