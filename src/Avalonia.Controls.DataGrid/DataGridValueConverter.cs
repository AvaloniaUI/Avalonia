// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Utils;
using Avalonia.Data.Converters;
using Avalonia.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Avalonia.Controls
{
    internal class DataGridValueConverter : IValueConverter
    {
        public static DataGridValueConverter Instance = new DataGridValueConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DefaultValueConverter.Instance.Convert(value, targetType, parameter, culture);
        }

        // This suppresses a warning saying that we should use String.IsNullOrEmpty instead of a string
        // comparison, but in this case we want to explicitly check for Empty and not Null.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != null && targetType.IsNullableType())
            {
                String strValue = value as String;
                if (strValue == String.Empty)
                {
                    return null;
                }
            }
            return DefaultValueConverter.Instance.ConvertBack(value, targetType, parameter, culture);
        }
    }
}
