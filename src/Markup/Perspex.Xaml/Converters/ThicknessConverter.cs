namespace Perspex.Xaml.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using OmniXaml.TypeConversion;

    public class ThicknessConverter : ITypeConverter
    {
        public object ConvertFrom(IXamlTypeConverterContext context, CultureInfo culture, object value)
        {
            var s = value as string;
            if (s != null)
            {
                return ConvertFromString(s);
            }

            return null;
        }

        private static Thickness ConvertFromString(string s)
        {
            var parts = s.Split(',')
                .Take(4)
                .Select(part => part.Trim());

            if (parts.Count() == 1)
            {
                var uniformLength = double.Parse(parts.First());
                return new Thickness(uniformLength);
            }

            double left = 0, top = 0, right = 0, bottom = 0;

            IList<Action<double>> setValue = new List<Action<double>>
            {
                val => left = val,
                val => top = val,
                val => right = val,
                val => bottom = val,
            };

            var i = 0;
            foreach (var part in parts)
            {
                var v = double.Parse(part);
                setValue[i](v);
                i++;
            }

            return new Thickness(left, top, right, bottom);
        }

        public object ConvertTo(IXamlTypeConverterContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new System.NotImplementedException();
        }

        public bool CanConvertTo(IXamlTypeConverterContext context, Type destinationType)
        {
            throw new NotImplementedException();
        }

        public bool CanConvertFrom(IXamlTypeConverterContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return false;
        }
    }
}