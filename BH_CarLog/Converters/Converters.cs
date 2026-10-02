using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace BH_CarLog.Converters
{
    /// <summary>bool → Visibility. Invert=true 면 반대로 동작한다.</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public Visibility FalseVisibility { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            if (Invert) flag = !flag;
            return flag ? Visibility.Visible : FalseVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible = value is Visibility v && v == Visibility.Visible;
            return Invert ? !visible : visible;
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;
    }

    /// <summary>null → Collapsed. Invert=true 면 null 일 때 Visible.</summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasValue = value != null && !(value is string s && string.IsNullOrEmpty(s));
            if (Invert) hasValue = !hasValue;
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>DateTime? → 문자열. ConverterParameter 로 포맷 지정 (기본 yyyy-MM-dd)</summary>
    public class DateFormatConverter : IValueConverter
    {
        public string DefaultFormat { get; set; } = "yyyy-MM-dd";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
                return dt.ToString(parameter as string ?? DefaultFormat);
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>byte[] → BitmapImage</summary>
    public class BytesToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not byte[] bytes || bytes.Length == 0)
                return null;
            try
            {
                var image = new BitmapImage();
                using var stream = new MemoryStream(bytes);
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>문자열이 비어있지 않으면 Visible</summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
