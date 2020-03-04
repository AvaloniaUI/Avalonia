using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Input;

namespace Avalonia.Controls.Converters
{
    public class PlatformKeyGestureConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return null;
            }
            else if (value is KeyGesture gesture && targetType == typeof(string))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return ToString(gesture, "Win");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return ToString(gesture, "Super");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return ToOSXString(gesture);
                }
                else
                {
                    return gesture.ToString();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static string ToString(KeyGesture gesture, string meta)
        {
            var s = new StringBuilder();

            static void Plus(StringBuilder s)
            {
                if (s.Length > 0)
                {
                    s.Append("+");
                }
            }

            if (gesture.KeyModifiers.HasFlagCustom(KeyModifiers.Control))
            {
                s.Append("Ctrl");
            }

            if (gesture.KeyModifiers.HasFlagCustom(KeyModifiers.Shift))
            {
                Plus(s);
                s.Append("Shift");
            }

            if (gesture.KeyModifiers.HasFlagCustom(KeyModifiers.Alt))
            {
                Plus(s);
                s.Append("Alt");
            }

            if (gesture.KeyModifiers.HasFlagCustom(KeyModifiers.Meta))
            {
                Plus(s);
                s.Append(meta);
            }

            Plus(s);
            s.Append(gesture.Key);

            return s.ToString();
        }

        private static string ToOSXString(KeyGesture gesture)
        {
            var s = new StringBuilder();

            if (gesture.KeyModifiers.HasFlagCustom(KeyModifiers.Control))
            {
                s.Append('⌃');
            }

            if (gesture.KeyModifiers.HasFlagCustom(KeyModifiers.Alt))
            {
                s.Append('⌥');
            }

            if (gesture.KeyModifiers.HasFlagCustom(KeyModifiers.Shift))
            {
                s.Append('⇧');
            }

            if (gesture.KeyModifiers.HasFlagCustom(KeyModifiers.Meta))
            {
                s.Append('⌘');
            }

            s.Append(gesture.Key);

            return s.ToString();
        }
    }
}
