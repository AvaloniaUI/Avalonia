using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Dialogs.Internal
{
    public class FileSizeStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long size && size > 0)
            {
                return Avalonia.Utilities.ByteSizeHelper.ToString((ulong)size, true);
            }

            return "";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
