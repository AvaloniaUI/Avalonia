using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.Markup.Xaml.Converters;
using Perspex.Media;
using Perspex.Media.Imaging;
using Perspex.Styling;
using Portable.Xaml.ComponentModel;

namespace Perspex.Markup.Xaml.Context
{
    public static class TypeConverterProvider
    {
        // HACK: For now these are hard-coded. Hopefully when the .NET Standard Platform
        // is available we can use the System.ComponentModel.TypeConverters so don't want to
        // spend time for now inventing a mechanism to register type converters if it's all
        // going to change.
        private static Dictionary<Type, Type> s_registered = new Dictionary<Type, Type>
        {
            { typeof(IBitmap), typeof(BitmapTypeConverter) },
            { typeof(Brush), typeof(BrushTypeConverter) },
            { typeof(Color), typeof(ColorTypeConverter) },
            { typeof(Classes), typeof(ClassesTypeConverter) },
            { typeof(ColumnDefinitions), typeof(ColumnDefinitionsTypeConverter) },
            { typeof(Geometry), typeof(GeometryTypeConverter) },
            { typeof(GridLength), typeof(GridLengthTypeConverter) },
            { typeof(KeyGesture), typeof(KeyGestureConverter) },
            { typeof(PerspexList<double>), typeof(PerspexListTypeConverter<double>) },
            { typeof(IMemberSelector), typeof(MemberSelectorTypeConverter) },
            { typeof(Point), typeof(PointTypeConverter) },
            { typeof(IList<Point>), typeof(PointsListTypeConverter) },
            { typeof(PerspexProperty), typeof(PerspexPropertyTypeConverter) },
            { typeof(RelativePoint), typeof(RelativePointTypeConverter) },
            { typeof(RelativeRect), typeof(RelativeRectTypeConverter) },
            { typeof(RowDefinitions), typeof(RowDefinitionsTypeConverter) },
            { typeof(Selector), typeof(SelectorTypeConverter) },
            { typeof(SolidColorBrush), typeof(SolidColorBrushTypeConverter) },
            { typeof(Thickness), typeof(ThicknessTypeConverter) },
            { typeof(TimeSpan), typeof(TimeSpanTypeConverter) },
            { typeof(Cursor), typeof(CursorTypeConverter) },
        };

        public static Type Find(Type type)
        {
            Type result;
            s_registered.TryGetValue(type, out result);
            return result;
        }
    }
}
