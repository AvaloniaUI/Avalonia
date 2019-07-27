using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace Avalonia.Dialogs
{
    internal class ResourceSelectorConverter : ResourceDictionary, IValueConverter
    {
        public object Convert(object key, Type targetType, object parameter, CultureInfo culture)
        {
            TryGetResource((string)key, out var value);
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
