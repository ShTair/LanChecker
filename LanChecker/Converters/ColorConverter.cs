using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LanChecker.Converters
{
    class ColorConverter : IValueConverter
    {
        private static Brush _red = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        private static Brush _green = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
        private static Brush _yellow = new SolidColorBrush(Color.FromArgb(100, 0, 255, 255));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((int)value)
            {
                case 0: return _green;
                case 1: return _yellow;
            }

            return _red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
