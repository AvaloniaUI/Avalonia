using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Diagnostics.Converters
{
    internal class PinnedToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.Equals("Pinned", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isPinned)
            {
                return isPinned ? "Pinned" : "Unpinned";
            }

            return "Unknown";
        }
    }
}
