using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Reactive;

namespace Avalonia.Diagnostics.Controls
{
    internal class PropertyValueEditor : Decorator
    {
        /// <summary>
        ///     Defines the <see cref="Value" /> property.
        /// </summary>
        public static readonly DirectProperty<PropertyValueEditor, object?> ValueProperty =
            AvaloniaProperty.RegisterDirect<PropertyValueEditor, object?>(
                nameof(Value), o => o.Value, (o, v) => o.Value = v);

        /// <summary>
        ///     Defines the <see cref="ValueType" /> property.
        /// </summary>
        public static readonly DirectProperty<PropertyValueEditor, Type?> ValueTypeProperty =
            AvaloniaProperty.RegisterDirect<PropertyValueEditor, Type?>(
                nameof(ValueType), o => o.ValueType, (o, v) => o.ValueType = v);

        /// <summary>
        ///     Defines the <see cref="IsReadonly" /> property.
        /// </summary>
        public static readonly DirectProperty<PropertyValueEditor, bool> IsReadonlyProperty =
            AvaloniaProperty.RegisterDirect<PropertyValueEditor, bool>(
                nameof(IsReadonly), o => o.IsReadonly, (o, v) => o.IsReadonly = v);

        private readonly CompositeDisposable _cleanup = new();

        private bool _isReadonly;
        private bool _needsUpdate;
        private object? _value;
        private Type? _valueType;

        public bool IsReadonly
        {
            get => _isReadonly;
            set => SetAndRaise(IsReadonlyProperty, ref _isReadonly, value);
        }

        public object? Value
        {
            get => _value;
            set => SetAndRaise(ValueProperty, ref _value, value);
        }

        public Type? ValueType
        {
            get => _valueType;
            set => SetAndRaise(ValueTypeProperty, ref _valueType, value);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _cleanup.Clear();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ValueTypeProperty)
            {
                _cleanup.Clear();

                _needsUpdate = true;
            }

            if (change.Property == ValueProperty && _needsUpdate)
            {
                _needsUpdate = false;

                Child = UpdateControl();
            }
        }

        //Unfortunately we cannot use TwoWay bindings as they update the source with the target value
        //This causes the source property value to be overwritten. Ideally we there would be some kind of
        //"InitialBindingDirection" or something to control whether the first value is from source or target.
        private static void TwoWayBindingFromSource(
            AvaloniaObject source,
            AvaloniaProperty sourceProperty,
            AvaloniaObject target,
            AvaloniaProperty targetProperty,
            IValueConverter? converter,
            Type targetType,
            CompositeDisposable disposable)
        {
            bool isUpdating = false;

            source
                .GetObservable(sourceProperty)
                .Subscribe(value =>
                {
                    if (isUpdating) return;

                    try
                    {
                        isUpdating = true;

                        target[targetProperty] = converter != null ?
                            converter.Convert(value, typeof(object), null, CultureInfo.CurrentCulture) :
                            value;
                    }
                    finally
                    {
                        isUpdating = false;
                    }
                })
                .DisposeWith(disposable);

            target
                .GetObservable(targetProperty)
                .Skip(1)
                .Subscribe(value =>
                {
                    if (isUpdating) return;

                    try
                    {
                        isUpdating = true;

                        source[sourceProperty] = converter != null ?
                            converter.ConvertBack(value, targetType, null, CultureInfo.CurrentCulture) :
                            value;
                    }
                    finally
                    {
                        isUpdating = false;
                    }
                })
                .DisposeWith(disposable);
        }

        private Control? UpdateControl()
        {
            if (ValueType is null) return null;

            TControl CreateControl<TControl>(AvaloniaProperty valueProperty,
                IValueConverter? converter = null,
                Action<TControl>? init = null)
                where TControl : Control, new()
            {
                var control = new TControl();

                init?.Invoke(control);

                TwoWayBindingFromSource(
                    this,
                    ValueProperty,
                    control,
                    valueProperty,
                    converter,
                    ValueType,
                    _cleanup);

                control.Bind(
                        IsEnabledProperty,
                        new Binding(nameof(IsReadonly)) { Source = this, Converter = BoolConverters.Not })
                    .DisposeWith(_cleanup);

                return control;
            }

            bool isObjectType = ValueType == typeof(object);

            if (ValueType == typeof(bool))
                return CreateControl<CheckBox>(ToggleButton.IsCheckedProperty);

            //TODO: Infinity, NaN not working with NumericUpDown
            //if (ValueType.IsPrimitive)
            //    return CreateControl<NumericUpDown>(NumericUpDown.ValueProperty, new ValueToDecimalConverter());

            if (ValueType == typeof(Color))
                return CreateControl<ColorPicker>(ColorView.ColorProperty);

            if (!isObjectType && ValueType.IsAssignableFrom(typeof(IBrush)))
                return CreateControl<BrushEditor>(BrushEditor.BrushProperty);

            if (!isObjectType && ValueType.IsAssignableFrom(typeof(IImage)))
                return CreateControl<Image>(Image.SourceProperty, init: img =>
                {
                    img.Stretch = Stretch.Uniform;
                    img.HorizontalAlignment = HorizontalAlignment.Stretch;

                    img.PointerPressed += (_, _) =>
                        new Window
                        {
                            Content = new Image
                            {
                                Source = img.Source
                            }
                        }.Show();
                });

            if (ValueType.IsEnum)
                return CreateControl<ComboBox>(
                    SelectingItemsControl.SelectedItemProperty, init: c =>
                    {
                        c.Items = Enum.GetValues(ValueType);
                    });

            var tb = CreateControl<TextBox>(
                TextBox.TextProperty,
                new TextToValueConverter(),
                t =>
                {
                    t.Watermark = "(null)";
                });

            tb.IsEnabled &= !isObjectType &&
                            StringConversionHelper.CanConvertFromString(ValueType);

            return tb;
        }

        private static class StringConversionHelper
        {
            private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
            private static readonly Type[] StringParameter = { typeof(string) };

            private static readonly Type[]
                StringFormatProviderParameters = { typeof(string), typeof(IFormatProvider) };

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

        private sealed class ValueToDecimalConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ToDecimal(value);
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return System.Convert.ChangeType(value, targetType);
            }
        }

        private sealed class TextToValueConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
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

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
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
