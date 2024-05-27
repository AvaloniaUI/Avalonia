using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Utilities;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converts a <see cref="KeyGesture"/> to a string, formatting it according to the current
    /// platform's style guidelines.
    /// </summary>
    public class PlatformKeyGestureConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return null;
            }
            else if (value is KeyGesture gesture && targetType == typeof(string))
            {
                return gesture.ToString("p", null);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts a <see cref="KeyGesture"/> to a string, formatting it according to the current
        /// platform's style guidelines.
        /// </summary>
        /// <param name="gesture">The gesture.</param>
        /// <returns>The gesture formatted according to the current platform.</returns>
        public static string ToPlatformString(KeyGesture gesture) => gesture.ToString("p", null);
        
    }
}
