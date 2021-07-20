using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Linq;

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
        public abstract string AssignedType { get; }
        public abstract string Value { get; set; }
        public abstract string Priority { get; }
        public abstract bool? IsAttached { get; }
        public abstract void Update();
        public abstract string PropertyType { get; }


        protected static string GetTypeName(Type type)
        {
            var name = type.Name;
            if (Nullable.GetUnderlyingType(type) is Type nullable)
            {
                name = nullable.Name + "?";
            }
            else if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                var arguments = type.GetGenericArguments();
                name = definition.Name.Substring(0, definition.Name.IndexOf('`'));
                name = $"{name}<{string.Join(",", arguments.Select(GetTypeName))}>";
            }
            return name;
        }

        protected static string ConvertToString(object? value)
        {
            if (value is null)
            {
                return "(null)";
            }

            var converter = TypeDescriptor.GetConverter(value);

            //CollectionConverter does not deliver any important information. It just displays "(Collection)".
            if (!converter.CanConvertTo(typeof(string)) ||
                converter.GetType() == typeof(CollectionConverter))
            {
                return value.ToString() ?? "(null)";
            }

            return converter.ConvertToString(value);
        }

        private static object? InvokeParse(string s, Type targetType)
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

            throw new InvalidCastException("Unable to convert value.");
        }

        protected static object? ConvertFromString(string s, Type targetType)
        {
            var converter = TypeDescriptor.GetConverter(targetType);

            if (converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFrom(null, CultureInfo.InvariantCulture, s);
            }

            return InvokeParse(s, targetType);
        }
    }
}
