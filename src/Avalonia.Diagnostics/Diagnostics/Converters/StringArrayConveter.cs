using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Diagnostics.Converters
{

    class StringArrayConveter : IValueConverter
    {
        readonly static string[] s_Empity = new string[0];
        public string Separator { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string[])
            {
                var array = (string[])value;
                return string.Join(Separator, array);
            }
            return value?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string @string)
            {
                if (string.IsNullOrWhiteSpace(@string))
                {
                    return s_Empity;
                }
                return @string.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            }
            return value;
        }
    }
}
