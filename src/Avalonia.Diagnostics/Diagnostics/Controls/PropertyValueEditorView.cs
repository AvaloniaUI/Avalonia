using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Reactive;
using Image = Avalonia.Controls.Image;

namespace Avalonia.Diagnostics.Controls
{
    internal class PropertyValueEditorView : UserControl
    {
        private static Geometry ImageGeometry = Geometry.Parse(
            "M12.25 6C8.79822 6 6 8.79822 6 12.25V35.75C6 37.1059 6.43174 38.3609 7.16525 39.3851L21.5252 25.0251C22.8921 23.6583 25.1081 23.6583 26.475 25.0251L40.8348 39.385C41.5683 38.3608 42 37.1058 42 35.75V12.25C42 8.79822 39.2018 6 35.75 6H12.25ZM34.5 17.5C34.5 19.7091 32.7091 21.5 30.5 21.5C28.2909 21.5 26.5 19.7091 26.5 17.5C26.5 15.2909 28.2909 13.5 30.5 13.5C32.7091 13.5 34.5 15.2909 34.5 17.5ZM39.0024 41.0881L24.7072 26.7929C24.3167 26.4024 23.6835 26.4024 23.293 26.7929L8.99769 41.0882C9.94516 41.6667 11.0587 42 12.25 42H35.75C36.9414 42 38.0549 41.6666 39.0024 41.0881Z");

        private readonly CompositeDisposable _cleanup = new();
        private PropertyViewModel? Property => (PropertyViewModel?)DataContext;

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            Content = UpdateControl();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _cleanup.Clear();
        }

        private Control? UpdateControl()
        {
            _cleanup.Clear();

            if (Property?.PropertyType is not { } propertyType) return null;

            TControl CreateControl<TControl>(AvaloniaProperty valueProperty,
                IValueConverter? converter = null,
                Action<TControl>? init = null)
                where TControl : Control, new()
            {
                var control = new TControl();

                init?.Invoke(control);

                control.Bind(valueProperty,
                    new Binding(nameof(Property.Value), BindingMode.TwoWay)
                    {
                        Source = Property, Converter = converter ?? new ValueConverter()
                    }).DisposeWith(_cleanup);

                control.IsEnabled = !Property.IsReadonly;

                return control;
            }

            bool isObjectType = propertyType == typeof(object);

            if (propertyType == typeof(bool))
                return CreateControl<CheckBox>(ToggleButton.IsCheckedProperty);

            //TODO: Infinity, NaN not working with NumericUpDown
            if (propertyType.IsPrimitive && propertyType != typeof(float) && propertyType != typeof(double))
                return CreateControl<NumericUpDown>(
                    NumericUpDown.ValueProperty,
                    new ValueToDecimalConverter(),
                    init: n =>
                    {
                        n.Increment = 1;
                        n.NumberFormat = new NumberFormatInfo { NumberDecimalDigits = 0 };
                        n.ParsingNumberStyle = NumberStyles.Integer;
                    });

            if (propertyType == typeof(Color))
                return CreateControl<ColorPicker>(ColorView.ColorProperty);

            if (!isObjectType && propertyType.IsAssignableFrom(typeof(IBrush)))
                return CreateControl<BrushEditor>(BrushEditor.BrushProperty);

            if (!isObjectType && propertyType.IsAssignableFrom(typeof(IImage)))
            {
                var valueObservable = Property.GetObservable(x => x.Value);

                var tbl = new TextBlock { VerticalAlignment = VerticalAlignment.Center };

                tbl.Bind(TextBlock.TextProperty,
                        valueObservable.Select(
                            value => value is IImage image ? $"{image.Size.Width} x {image.Size.Height}" : "(null)"))
                    .DisposeWith(_cleanup);

                var sp = new StackPanel
                {
                    Background = Brushes.Transparent,
                    Orientation = Orientation.Horizontal,
                    Spacing = 2,
                    Children =
                    {
                        new Path
                        {
                            Data = ImageGeometry,
                            Fill = Brushes.Gray,
                            Width = 12,
                            Height = 12,
                            Stretch = Stretch.Uniform,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        tbl
                    }
                };

                var previewImage = new Image
                {
                    Width = 300,
                    Height = 300
                };

                previewImage
                    .Bind(Image.SourceProperty, valueObservable)
                    .DisposeWith(_cleanup);

                ToolTip.SetTip(sp, previewImage);

                return sp;
            }

            if (propertyType.IsEnum)
                return CreateControl<ComboBox>(
                    SelectingItemsControl.SelectedItemProperty, init: c =>
                    {
                        c.Items = Enum.GetValues(propertyType);
                    });

            var tb = CreateControl<TextBox>(
                TextBox.TextProperty,
                new TextToValueConverter(),
                t =>
                {
                    t.Watermark = "(null)";
                });

            tb.IsEnabled &= !isObjectType &&
                            StringConversionHelper.CanConvertFromString(propertyType);

            return tb;
        }

        //HACK: ValueConverter that skips first target update
        private class ValueConverter : IValueConverter
        {
            private bool _firstUpdate = true;

            protected virtual object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return value;
            }

            protected virtual object? ConvertBack(object? value, Type targetType, object? parameter,
                CultureInfo culture)
            {
                return value;
            }

            object? IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return Convert(value, targetType, parameter, culture);
            }

            object? IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (_firstUpdate)
                {
                    _firstUpdate = false;

                    return BindingOperations.DoNothing;
                }

                return ConvertBack(value, targetType, parameter, culture);
            }
        }

        private static class StringConversionHelper
        {
            private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
            private static readonly Type[] StringParameter = { typeof(string) };
            private static readonly Type[] StringFormatProviderParameters = { typeof(string), typeof(IFormatProvider) };

            public static bool CanConvertFromString(Type type)
            {
                var converter = TypeDescriptor.GetConverter(type);

                if (converter.CanConvertFrom(typeof(string))) return true;

                return GetParseMethod(type, out _) != null;
            }

            public static MethodInfo? GetParseMethod(Type type, out bool hasFormat)
            {
                var m = type.GetMethod("Parse", PublicStatic, null, StringFormatProviderParameters, null);

                if (m != null)
                {
                    hasFormat = true;

                    return m;
                }

                hasFormat = false;

                return type.GetMethod("Parse", PublicStatic, null, StringParameter, null);
            }
        }

        private sealed class ValueToDecimalConverter : ValueConverter
        {
            protected override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToDecimal(value);
            }

            protected override object? ConvertBack(object? value, Type targetType, object? parameter,
                CultureInfo culture)
            {
                return System.Convert.ChangeType(value, targetType);
            }
        }

        private sealed class TextToValueConverter : ValueConverter
        {
            protected override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (value is null)
                    return null;

                var converter = TypeDescriptor.GetConverter(value);

                //CollectionConverter does not deliver any important information. It just displays "(Collection)".
                if (!converter.CanConvertTo(typeof(string)) ||
                    converter.GetType() == typeof(CollectionConverter))
                    return value.ToString();

                return converter.ConvertToString(value);
            }

            protected override object? ConvertBack(object? value, Type targetType, object? parameter,
                CultureInfo culture)
            {
                if (value is not string s)
                    return null;

                try
                {
                    var converter = TypeDescriptor.GetConverter(targetType);

                    return converter.CanConvertFrom(typeof(string)) ?
                        converter.ConvertFrom(null, CultureInfo.InvariantCulture, s) :
                        InvokeParse(s, targetType);
                }
                catch
                {
                    return BindingOperations.DoNothing;
                }
            }

            private static object? InvokeParse(string s, Type targetType)
            {
                var m = StringConversionHelper.GetParseMethod(targetType, out bool hasFormat);

                if (m == null) throw new InvalidOperationException();

                return m.Invoke(null,
                    hasFormat ?
                        new object[] { s, CultureInfo.InvariantCulture } :
                        new object[] { s });
            }
        }
    }
}
