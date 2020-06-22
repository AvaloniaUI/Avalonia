using System;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters
{
	using System.ComponentModel;

    public class TimeSpanTypeConverter : System.ComponentModel.TimeSpanConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var valueStr = (string)value;
            if (!valueStr.Contains(":"))
            {
                // shorthand seconds format (ie. "0.25")
                var secs = double.Parse(valueStr, CultureInfo.InvariantCulture);
                return TimeSpan.FromSeconds(secs);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}