// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Perspex;
using Perspex.Markup;

namespace XamlTestApplication
{
    public class StringNullOrEmpty : IValueConverter
    {
        public static readonly StringNullOrEmpty Instance = new StringNullOrEmpty();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return true;
            }
            else
            {
                var s = value as string;
                return s != null ? string.IsNullOrEmpty(s) : PerspexProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
