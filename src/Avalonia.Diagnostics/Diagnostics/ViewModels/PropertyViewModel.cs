using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class PropertyViewModel : ViewModelBase
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private static readonly Type[] StringParameter = { typeof(string) };
        private static readonly Type[] StringIFormatProviderParameters = { typeof(string), typeof(IFormatProvider) };

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

            //Check if there's an user provided ToString(), prefer that over the TypeDescriptor conversion
            if (value.GetType().GetMethod(nameof(ToString), System.Type.EmptyTypes)
                .DeclaringType != typeof(object))
            {
                return value.ToString();
            }

            try
            {
                var converter = TypeDescriptor.GetConverter(value);

                return converter.ConvertToString(value);
            }
            catch
            {
                return value.ToString();
            }
        }

        protected static object ConvertFromString(string s, Type targetType)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(targetType);

                if (converter.CanConvertFrom(typeof(string)))
                {
                    return converter.ConvertFrom(null, CultureInfo.InvariantCulture, s);
                }
            }
            catch
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
