using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Controls.Templates;

namespace Avalonia.Markup.Xaml
{
    using Avalonia.Media;

    /// <summary>
    /// Maintains a repository of <see cref="TypeConverter"/>s for XAML parsing on top of those
    /// maintained by <see cref="TypeDescriptor"/>.
    /// </summary>
    /// <remarks>
    /// The default method of defining type converters using <see cref="TypeConverterAttribute"/>
    /// isn't powerful enough for our purposes:
    /// 
    /// - It doesn't handle non-constructed generic types (such as <see cref="AvaloniaList{T}"/>)
    /// - Type converters which require XAML features cannot be defined in non-XAML assemblies and
    ///   so can't be referenced using <see cref="TypeConverterAttribute"/>
    /// - Many types have a static `Parse(string)` method which can be used implicitly; this class
    ///   detects such methods and auto-creates a type converter
    /// </remarks>
    public static class AvaloniaTypeConverters
    {
        private static Dictionary<Type, Type> _converters = new Dictionary<Type, Type>()
        {
            { typeof(AvaloniaList<>), typeof(AvaloniaListConverter<>) },
            { typeof(AvaloniaProperty), typeof(AvaloniaPropertyTypeConverter) },
            { typeof(IBitmap), typeof(BitmapTypeConverter) },
            { typeof(IList<Point>), typeof(PointsListTypeConverter) },
            { typeof(IMemberSelector), typeof(MemberSelectorTypeConverter) },
            { typeof(Selector), typeof(SelectorTypeConverter) },
            { typeof(TimeSpan), typeof(TimeSpanTypeConverter) },
            { typeof(WindowIcon), typeof(IconTypeConverter) },
            { typeof(CultureInfo), typeof(CultureInfoConverter) },
            { typeof(Uri), typeof(AvaloniaUriTypeConverter) },
            { typeof(FontFamily), typeof(FontFamilyTypeConverter) }
        };

        /// <summary>
        /// Tries to lookup a <see cref="TypeConverter"/> for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type converter.</returns>
        public static Type GetTypeConverter(Type type)
        {
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var inner = GetTypeConverter(type.GetGenericArguments()[0]);
                if (inner == null)
                    return null;
                return typeof(NullableTypeConverter<>).MakeGenericType(inner);
            }
            
            if (_converters.TryGetValue(type, out var result))
            {
                return result;
            }

            // Converters for non-constructed generic types can't be specified using
            // TypeConverterAttribute. Allow them to be registered here and handle them sanely.
            if (type.IsConstructedGenericType &&
                _converters.TryGetValue(type.GetGenericTypeDefinition(), out result))
            {
                return result?.MakeGenericType(type.GetGenericArguments());
            }

            // If the type isn't a primitive or a type that XAML already handles, but has a static
            // Parse method, use that
            if (!type.IsPrimitive &&
                type != typeof(DateTime) &&
                type != typeof(Uri) &&
                ParseTypeConverter.HasParseMethod(type))
            {
                result = typeof(ParseTypeConverter<>).MakeGenericType(type);
                _converters.Add(type, result);
                return result;
            }

            _converters.Add(type, null);
            return null;
        }

        /// <summary>
        /// Registers a type converter for a type.
        /// </summary>
        /// <param name="type">The type. Maybe be a non-constructed generic type.</param>
        /// <param name="converterType">The converter type. Maybe be a non-constructed generic type.</param>
        public static void Register(Type type, Type converterType) => _converters[type] = converterType;
    }
}
