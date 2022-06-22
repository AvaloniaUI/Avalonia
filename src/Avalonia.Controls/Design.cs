
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    public static class Design
    {
        private static Dictionary<object, Control?>? _previewWith;

        public static bool IsDesignMode { get; internal set; }

        public static readonly AttachedProperty<double> HeightProperty = AvaloniaProperty
            .RegisterAttached<Control, double>("Height", typeof (Design));

        public static void SetHeight(Control control, double value)
        {
            control.SetValue(HeightProperty, value);
        }

        public static double GetHeight(Control control)
        {
            return control.GetValue(HeightProperty);
        }

        public static readonly AttachedProperty<double> WidthProperty = AvaloniaProperty
            .RegisterAttached<Control, double>("Width", typeof(Design));

        public static void SetWidth(Control control, double value)
        {
            control.SetValue(WidthProperty, value);
        }

        public static double GetWidth(Control control)
        {
            return control.GetValue(WidthProperty);
        }

        public static readonly AttachedProperty<object> DataContextProperty = AvaloniaProperty
            .RegisterAttached<Control, object>("DataContext", typeof (Design));

        public static void SetDataContext(Control control, object value)
        {
            control.SetValue(DataContextProperty, value);
        }

        public static object GetDataContext(Control control)
        {
            return control.GetValue(DataContextProperty);
        }
        
        public static readonly AttachedProperty<Control?> PreviewWithProperty = AvaloniaProperty
            .RegisterAttached<AvaloniaObject, Control?>("PreviewWith", typeof (Design));

        public static void SetPreviewWith(AvaloniaObject target, Control? control)
        {
            target.SetValue(PreviewWithProperty, control);
        }

        public static void SetPreviewWith(ResourceDictionary target, Control? control)
        {
            _previewWith ??= new();
            _previewWith[target] = control;
        }

        public static Control? GetPreviewWith(AvaloniaObject target)
        {
            return target.GetValue(PreviewWithProperty);
        }

        public static Control? GetPreviewWith(ResourceDictionary target)
        {
            return _previewWith?[target];
        }

        public static readonly AttachedProperty<IStyle> DesignStyleProperty = AvaloniaProperty
            .RegisterAttached<Control, IStyle>("DesignStyle", typeof(Design));

        public static void SetDesignStyle(Control control, IStyle value)
        {
            control.SetValue(DesignStyleProperty, value);
        }

        public static IStyle GetDesignStyle(Control control)
        {
            return control.GetValue(DesignStyleProperty);
        }

        public static void ApplyDesignModeProperties(Control target, Control source)
        {
            if (source.IsSet(WidthProperty))
                target.Width = source.GetValue(WidthProperty);
            if (source.IsSet(HeightProperty))
                target.Height = source.GetValue(HeightProperty);
            if (source.IsSet(DataContextProperty))
                target.DataContext = source.GetValue(DataContextProperty);
            if (source.IsSet(DesignStyleProperty))
                target.Styles.Add(source.GetValue(DesignStyleProperty));
        }
    }
}
