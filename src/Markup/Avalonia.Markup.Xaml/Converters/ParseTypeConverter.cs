using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Avalonia.Markup.Xaml.Converters
{
    /// <summary>
    /// Base class for type converters which call a static Parse method.
    /// </summary>
    public abstract class ParseTypeConverter : TypeConverter
    {
        protected const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        protected static readonly Type[] StringParameter = new[] { typeof(string) };
        protected static readonly Type[] StringIFormatProviderParameters = new[] { typeof(string), typeof(IFormatProvider) };

        /// <summary>
        /// Checks whether a type has a suitable Parse method.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if the type has a suitable parse method, otherwise false.</returns>
        public static bool HasParseMethod(Type type)
        {
            return type.GetMethod("Parse", PublicStatic, null, StringIFormatProviderParameters, null) != null ||
                   type.GetMethod("Parse", PublicStatic, null, StringParameter, null) != null;
        }
    }

    /// <summary>
    /// A type converter which calls a static Parse method.
    /// </summary>
    /// <typeparam name="T">The type with the Parse method.</typeparam>
    public class ParseTypeConverter<T> : ParseTypeConverter
    {
        private static Func<string, T> _parse;
        private static Func<string, IFormatProvider, T> _parseWithFormat;

        static ParseTypeConverter()
        {
            var method = typeof(T).GetMethod("Parse", PublicStatic, null, StringIFormatProviderParameters, null);

            if (method != null)
            {
                _parseWithFormat = (Func<string, IFormatProvider, T>)method
                    .CreateDelegate(typeof(Func<string, IFormatProvider, T>));
                return;
            }

            method = typeof(T).GetMethod("Parse", PublicStatic, null, StringParameter, null);

            if (method != null)
            {
                _parse = (Func<string, T>)method.CreateDelegate(typeof(Func<string, T>));
            }
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value != null)
            {
                if (_parse != null)
                {
                    return _parse(value.ToString());
                }
                else if (_parseWithFormat != null)
                {
                    return _parseWithFormat(value.ToString(), culture);
                }
            }

            return null;
        }
    }
}
