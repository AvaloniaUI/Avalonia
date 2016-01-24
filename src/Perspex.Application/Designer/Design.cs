using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls;

namespace Perspex
{
    public static class Design
    {
        public static bool IsDesignMode { get; internal set; }

        public static readonly PerspexProperty<double> HeightProperty = PerspexProperty
            .RegisterAttached<Control, double>("Height", typeof (Design));

        public static void SetHeight(Control control, double value)
        {
            control.SetValue(HeightProperty, value);
        }

        public static double GetHeight(Control control)
        {
            return control.GetValue(HeightProperty);
        }

        public static readonly PerspexProperty<double> WidthProperty = PerspexProperty
    .RegisterAttached<Control, double>("Width", typeof(Design));

        public static void SetWidth(Control control, double value)
        {
            control.SetValue(WidthProperty, value);
        }

        public static double GetWidth(Control control)
        {
            return control.GetValue(WidthProperty);
        }

        public static readonly PerspexProperty<object> DataContextProperty = PerspexProperty
            .RegisterAttached<Control, object>("DataContext", typeof (Design));

        public static void SetDataContext(Control control, object value)
        {
            control.SetValue(DataContextProperty, value);
        }

        public static object GetDataContext(Control control)
        {
            return control.GetValue(DataContextProperty);
        }
        
        internal static void ApplyDesignerProperties(Control target, Control source)
        {
            if (source.IsSet(WidthProperty))
                target.Width = source.GetValue(WidthProperty);
            if (source.IsSet(HeightProperty))
                target.Height = source.GetValue(HeightProperty);
            if (source.IsSet(DataContextProperty))
                target.DataContext = source.GetValue(DataContextProperty);
        }
    }
}
