using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Diagnostics.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.Reactive;

namespace Avalonia.Diagnostics.Views
{
    internal class PropertyValueEditorView : UserControl
    {
        private static readonly Geometry ImageIcon = Geometry.Parse(
            "M12.25 6C8.79822 6 6 8.79822 6 12.25V35.75C6 37.1059 6.43174 38.3609 7.16525 39.3851L21.5252 25.0251C22.8921 23.6583 25.1081 23.6583 26.475 25.0251L40.8348 39.385C41.5683 38.3608 42 37.1058 42 35.75V12.25C42 8.79822 39.2018 6 35.75 6H12.25ZM34.5 17.5C34.5 19.7091 32.7091 21.5 30.5 21.5C28.2909 21.5 26.5 19.7091 26.5 17.5C26.5 15.2909 28.2909 13.5 30.5 13.5C32.7091 13.5 34.5 15.2909 34.5 17.5ZM39.0024 41.0881L24.7072 26.7929C24.3167 26.4024 23.6835 26.4024 23.293 26.7929L8.99769 41.0882C9.94516 41.6667 11.0587 42 12.25 42H35.75C36.9414 42 38.0549 41.6666 39.0024 41.0881Z");

        private static readonly Geometry GeometryIcon = Geometry.Parse(
            "M23.25 15.5H30.8529C29.8865 8.99258 24.2763 4 17.5 4C10.0442 4 4 10.0442 4 17.5C4 24.2763 8.99258 29.8865 15.5 30.8529V23.25C15.5 18.9698 18.9698 15.5 23.25 15.5ZM23.25 18C20.3505 18 18 20.3505 18 23.25V38.75C18 41.6495 20.3505 44 23.25 44H38.75C41.6495 44 44 41.6495 44 38.75V23.25C44 20.3505 41.6495 18 38.75 18H23.25Z");

        private static readonly ColorToBrushConverter Color2Brush = new();

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

        private static bool ImplementsInterface<TInterface>(Type type)
        {
            var interfaceType = typeof(TInterface);
            return type == interfaceType || interfaceType.IsAssignableFrom(type);
        }

        private Control? UpdateControl()
        {
            _cleanup.Clear();

            if (Property?.PropertyType is not { } propertyType)
                return null;

            if (propertyType == typeof(bool))
                return CreateControl<CheckBox>(ToggleButton.IsCheckedProperty);

            //TODO: Infinity, NaN not working with NumericUpDown
            if (IsValidNumeric(propertyType))
                return CreateControl<NumericUpDown>(
                    NumericUpDown.ValueProperty,
                    new ValueToDecimalConverter(),
                    init: n =>
                    {
                        n.Increment = 1;
                        n.NumberFormat = new NumberFormatInfo { NumberDecimalDigits = 0 };
                        n.ParsingNumberStyle = NumberStyles.Integer;
                    },
                    readonlyProperty: NumericUpDown.IsReadOnlyProperty);

            if (propertyType == typeof(Color))
            {
                var el = new Ellipse { Width = 12, Height = 12, VerticalAlignment = VerticalAlignment.Center };

                el.Bind(
                        Shape.FillProperty,
                        new Binding(nameof(Property.Value)) { Source = Property, Converter = Color2Brush })
                    .DisposeWith(_cleanup);

                var tbl = new TextBlock { VerticalAlignment = VerticalAlignment.Center };

                tbl.Bind(
                        TextBlock.TextProperty,
                        new Binding(nameof(Property.Value)) { Source = Property })
                    .DisposeWith(_cleanup);

                var sp = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 2,
                    Children = { el, tbl },
                    Background = Brushes.Transparent,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    IsEnabled = !Property.IsReadonly
                };

                var cv = new ColorView
                {
                    HexInputAlphaPosition = AlphaComponentPosition.Leading, // Always match XAML
                };

                cv.Bind(
                        ColorView.ColorProperty,
                        new Binding(nameof(Property.Value), BindingMode.TwoWay)
                        {
                            Source = Property,
                            Converter = Color2Brush
                        })
                    .DisposeWith(_cleanup);

                FlyoutBase.SetAttachedFlyout(sp, new Flyout { Content = cv });

                sp.PointerPressed += (_, _) => FlyoutBase.ShowAttachedFlyout(sp);

                return sp;
            }

            if (ImplementsInterface<IBrush>(propertyType))
                return CreateControl<BrushEditor>(BrushEditor.BrushProperty);

            var isImage = ImplementsInterface<IImage>(propertyType);
            var isGeometry = propertyType == typeof(Geometry);

            if (isImage || isGeometry)
            {
                var valueObservable = Property.GetObservable(x => x.Value);
                var tbl = new TextBlock { VerticalAlignment = VerticalAlignment.Center };

                tbl.Bind(TextBlock.TextProperty,
                        valueObservable.Select(
                            value => value switch
                            {
                                IImage img => $"{img.Size.Width} x {img.Size.Height}",
                                Geometry geom => $"{geom.Bounds.Width} x {geom.Bounds.Height}",
                                _ => "(null)"
                            }))
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
                            Data = isImage ? ImageIcon : GeometryIcon,
                            Fill = Brushes.Gray,
                            Width = 12,
                            Height = 12,
                            Stretch = Stretch.Uniform,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        tbl
                    }
                };

                if (isImage)
                {
                    var previewImage = new Image { Stretch = Stretch.Uniform, Width = 300, Height = 300 };

                    previewImage
                        .Bind(Image.SourceProperty, valueObservable)
                        .DisposeWith(_cleanup);

                    ToolTip.SetTip(sp, previewImage);
                }
                else
                {
                    var previewShape = new Path
                    {
                        Stretch = Stretch.Uniform,
                        Fill = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    previewShape
                        .Bind(Path.DataProperty, valueObservable)
                        .DisposeWith(_cleanup);

                    ToolTip.SetTip(sp, new Border { Child = previewShape, Width = 300, Height = 300 });
                }

                return sp;
            }

            if (propertyType.IsEnum)
                return CreateControl<ComboBox>(
                    SelectingItemsControl.SelectedItemProperty, init: c =>
                    {
                        c.ItemsSource = Enum.GetValues(propertyType);
                    });

            var tb = CreateControl<CommitTextBox>(
                CommitTextBox.CommittedTextProperty,
                new TextToValueConverter(),
                t =>
                {
                    t.Watermark = "(null)";
                },
                readonlyProperty: TextBox.IsReadOnlyProperty);

            tb.IsReadOnly |= propertyType == typeof(object) ||
                             !StringConversionHelper.CanConvertFromString(propertyType);

            if (!tb.IsReadOnly)
            {
                tb.GetObservable(TextBox.TextProperty).Subscribe(t =>
                {
                    try
                    {
                        if (t != null)
                        {
                            StringConversionHelper.FromString(t, propertyType);
                        }

                        DataValidationErrors.ClearErrors(tb);
                    }
                    catch (Exception ex)
                    {
                        DataValidationErrors.SetError(tb, ex.GetBaseException());
                    }
                }).DisposeWith(_cleanup);
            }

            return tb;

            TControl CreateControl<TControl>(AvaloniaProperty valueProperty,
                    IValueConverter? converter = null,
                    Action<TControl>? init = null,
                    AvaloniaProperty? readonlyProperty = null)
                    where TControl : Control, new()
            {
                var control = new TControl();
                var bindingMode = Property.IsReadonly ? BindingMode.OneWay : BindingMode.TwoWay;

                init?.Invoke(control);

                control.Bind(valueProperty,
                    new Binding(nameof(Property.Value), bindingMode)
                    {
                        Source = Property,
                        Converter = converter ?? new ValueConverter(),
                        ConverterParameter = propertyType
                    }).DisposeWith(_cleanup);

                if (readonlyProperty != null)
                {
                    control[readonlyProperty] = Property.IsReadonly;
                }
                else
                {
                    control.IsEnabled = !Property.IsReadonly;
                }

                return control;
            }

            static bool IsValidNumeric(Type? type)
            {
                if (type == null || type.IsEnum == true)
                {
                    return false;
                }
                var typeCode = Type.GetTypeCode(type);
                if (typeCode == TypeCode.Object)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        typeCode = Type.GetTypeCode(Nullable.GetUnderlyingType(type));
                    }
                    else
                    {
                        return false;
                    }
                }
                switch (typeCode)
                {
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                    default:
                        return false;
                }
            }


        }

        //HACK: ValueConverter that skips first target update
        //TODO: Would be nice to have some kind of "InitialBindingValue" option on TwoWay bindings to control
        //if the first value comes from the source or target
        private class ValueConverter : IValueConverter
        {
            private bool _firstUpdate = true;

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

                //Note: targetType provided by Converter is simply "object"
                return ConvertBack(value, (Type)parameter!, parameter, culture);
            }

            protected virtual object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return value;
            }

            protected virtual object? ConvertBack(object? value, Type targetType, object? parameter,
                CultureInfo culture)
            {
                return value;
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

                if (converter.CanConvertFrom(typeof(string)))
                    return true;

                return GetParseMethod(type, out _) != null;
            }

            public static string? ToString(object o)
            {
                var converter = TypeDescriptor.GetConverter(o);

                //CollectionConverter does not deliver any important information. It just displays "(Collection)".
                if (!converter.CanConvertTo(typeof(string)) ||
                    converter.GetType() == typeof(CollectionConverter))
                    return o.ToString();

                return converter.ConvertToInvariantString(o);
            }

            public static object? FromString(string str, Type type)
            {
                var converter = TypeDescriptor.GetConverter(type);

                return converter.CanConvertFrom(typeof(string)) ?
                    converter.ConvertFrom(null, CultureInfo.InvariantCulture, str) :
                    InvokeParse(str, type);
            }

            private static object? InvokeParse(string s, Type targetType)
            {
                var m = GetParseMethod(targetType, out var hasFormat);

                if (m == null)
                    throw new InvalidOperationException();

                return m.Invoke(null,
                    hasFormat ?
                        new object[] { s, CultureInfo.InvariantCulture } :
                        new object[] { s });
            }

            private static MethodInfo? GetParseMethod(Type type, out bool hasFormat)
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
                return value is null ? null : StringConversionHelper.ToString(value);
            }

            protected override object? ConvertBack(object? value, Type targetType, object? parameter,
                CultureInfo culture)
            {
                if (value is not string s)
                    return null;

                try
                {
                    return StringConversionHelper.FromString(s, targetType);
                }
                catch
                {
                    return BindingOperations.DoNothing;
                }
            }
        }
    }
}
