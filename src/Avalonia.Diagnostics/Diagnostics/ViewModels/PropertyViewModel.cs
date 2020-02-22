using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class PropertyViewModel : ViewModelBase
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private static readonly Type[] StringParameter = new[] { typeof(string) };
        private static readonly Type[] StringIFormatProviderParameters = new[] { typeof(string), typeof(IFormatProvider) };

        public abstract object Key { get; }
        public abstract string Name { get; }
        public abstract string Group { get; }
        public abstract string Type { get; }
        public abstract string Value { get; set; }
        public abstract void Update();

        protected static string ConvertToString(object value)
        {
            if (value is null)
            {
                return "(null)";
            }

            var converter = TypeDescriptor.GetConverter(value);
            return converter?.ConvertToString(value) ?? value.ToString();
        }

        protected static object ConvertFromString(string s, Type targetType)
        {
            var converter = TypeDescriptor.GetConverter(targetType);
            
            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFrom(null, CultureInfo.InvariantCulture, s);
            }
            else
            {
                var method = targetType.GetMethod("Parse", PublicStatic, null, StringIFormatProviderParameters, null);

                if (method != null)
                {
                    return method.Invoke(null, new object[] { s, CultureInfo.InvariantCulture });
                }

                method = targetType.GetMethod("Parse", PublicStatic, null, StringParameter, null);

                if (method != null)
                {
                    return method.Invoke(null, new object[] { s });
                }
            }

            throw new InvalidCastException("Unable to convert value.");
        }
    }
}
