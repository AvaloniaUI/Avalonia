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
        private static MethodInfo _parseMethod;
        private static bool _acceptsCulture;

        static ParseTypeConverter()
        {
            _parseMethod = typeof(T).GetMethod("Parse", PublicStatic, null, StringIFormatProviderParameters, null);
            _acceptsCulture = _parseMethod != null;

            if (_parseMethod == null)
            {
                _parseMethod = typeof(T).GetMethod("Parse", PublicStatic, null, StringParameter, null);
            }
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return _parseMethod != null && sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value != null && _parseMethod != null)
            {
                return _acceptsCulture ?
                    _parseMethod.Invoke(null, new[] { value, culture }) :
                    _parseMethod.Invoke(null, new[] { value });
            }

            return null;
        }
    }
}
