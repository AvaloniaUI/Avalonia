using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Portable.Xaml.ComponentModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Collections;
using Avalonia.Controls.Templates;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    public class AvaloniaDefaultTypeConverters
    {
        private static Dictionary<Type, Type> _defaultConverters = new Dictionary<Type, Type>()
        {
            //avalonia default converters
            { typeof(IBitmap), typeof(BitmapTypeConverter)},
            { typeof(IBrush), typeof(BrushTypeConverter) },
            { typeof(Color), typeof(ColorTypeConverter) },
            { typeof(Classes), typeof(ClassesTypeConverter) },
            { typeof(ColumnDefinitions), typeof(ColumnDefinitionsTypeConverter) },
            //{ typeof(DateTime), typeof(DateTimeTypeConverter) },
            { typeof(Geometry), typeof(GeometryTypeConverter) },
            { typeof(GridLength), typeof(GridLengthTypeConverter) },
            { typeof(KeyGesture), typeof(KeyGestureConverter) },
            { typeof(AvaloniaList<double>), typeof(AvaloniaListTypeConverter<double>) },
            { typeof(IMemberSelector), typeof(MemberSelectorTypeConverter) },
            { typeof(Point), typeof(PointTypeConverter) },
            { typeof(Matrix), typeof(MatrixTypeConverter) },
            { typeof(IList<Point>), typeof(PointsListTypeConverter) },
            { typeof(AvaloniaProperty), typeof(AvaloniaPropertyTypeConverter) },
            { typeof(RelativePoint), typeof(RelativePointTypeConverter) },
            { typeof(RelativeRect), typeof(RelativeRectTypeConverter) },
            { typeof(RowDefinitions), typeof(RowDefinitionsTypeConverter) },
            { typeof(Size), typeof(SizeTypeConverter) },
            { typeof(Rect), typeof(RectTypeConverter) },
            { typeof(Selector), typeof(SelectorTypeConverter)},
            { typeof(SolidColorBrush), typeof(BrushTypeConverter) },
            { typeof(Thickness), typeof(ThicknessTypeConverter) },
            { typeof(TimeSpan), typeof(TimeSpanTypeConverter) },
            //{ typeof(Uri), typeof(Converters.UriTypeConverter) },
            { typeof(Cursor), typeof(CursorTypeConverter) },
            { typeof(WindowIcon), typeof(IconTypeConverter) },
            //{ typeof(FontWeight), typeof(FontWeightConverter) },
        };

        public static Type GetTypeConverter(Type type)
        {
            Type converterType;

            if (_defaultConverters.TryGetValue(type, out converterType))
            {
                return converterType;
            }

            //TODO: probably a smarter way to handle types
            //is it needed ??
            //Type curType = type;

            //while (curType != null)
            //{
            //    if (_defaultConverters.TryGetValue(curType, out converterType))
            //    {
            //        if (curType != type)
            //        {
            //            _defaultConverters[curType] = converterType;
            //        }
            //        return converterType;
            //    }
            //    else
            //    {
            //        _defaultConverters[curType] = null;
            //    }
            //    curType = curType.GetTypeInfo().BaseType;
            //}



            return null;
        }
    }
}