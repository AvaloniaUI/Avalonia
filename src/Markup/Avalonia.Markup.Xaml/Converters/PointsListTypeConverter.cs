using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.Converters
{
    public class PointsListTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            var points = new List<Point>();

            using (var tokenizer = new StringTokenizer((string)value, CultureInfo.InvariantCulture, exceptionMessage: "Invalid PointsList."))
            {
                while (tokenizer.TryReadDouble(out double x))
                {
                    points.Add(new Point(x, tokenizer.ReadDouble()));
                }
            }

            return points;
        }
    }
}
