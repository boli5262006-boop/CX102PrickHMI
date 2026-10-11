using System;
using System.Globalization;
using System.Windows.Data;

namespace CX102PrickHMI.Converters
{
    /// <summary>
    /// 搭接仪表数值格式化：验证模式开启时显示 2 位小数，否则 1 位。
    /// 输入：[0] 数值 (double)，[1] 是否验证模式 (bool)。
    /// </summary>
    public sealed class SpliceValueFormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || !(values[0] is double) || !(values[1] is bool))
            {
                return string.Empty;
            }

            var value = (double)values[0];
            var format = (bool)values[1] ? "F2" : "F1";
            return value.ToString(format, CultureInfo.InvariantCulture) + "mm";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
