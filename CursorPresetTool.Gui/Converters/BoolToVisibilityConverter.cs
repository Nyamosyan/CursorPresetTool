using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CursorPresetTool.Gui.Converters
{
    /// <summary>
    /// bool → Visibility
    /// true = Visible / false = Collapsed
    /// </summary>
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }
}
