using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Logging;
using Avalonia.Styling;

namespace Avalonia.Data
{
    /// <summary>
    /// A XAML binding to a property on a control's templated parent.
    /// </summary>
    public partial class TemplateBinding : UntypedBindingExpressionBase,
        IBinding,
        IBinding2,
        IDescription,
        ISetterValue,
        IDisposable
    {
        private bool _isSetterValue;

        public TemplateBinding()
            : base(BindingPriority.Template)
        {
        }

        public TemplateBinding(AvaloniaProperty property)
            : base(BindingPriority.Template)
        {
            Property = property;
        }

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IValueConverter? Converter { get; set; }

        /// <summary>
        /// Gets or sets the culture in which to evaluate the converter.
        /// </summary>
        /// <value>The default value is null.</value>
        /// <remarks>
        /// If this property is not set then <see cref="CultureInfo.CurrentCulture"/> will be used.
        /// </remarks>
        [TypeConverter(typeof(CultureInfoIetfLanguageTagConverter))]
        public CultureInfo? ConverterCulture { get; set; }

        /// <summary>
        /// Gets or sets a parameter to pass to <see cref="Converter"/>.
        /// </summary>
        public object? ConverterParameter { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public new BindingMode Mode 
        { 
            get => base.Mode;
            set => base.Mode = value;
        }

        /// <summary>
        /// Gets or sets the name of the source property on the templated parent.
        /// </summary>
        public AvaloniaProperty? Property { get; set; }

        /// <inheritdoc/>
        public override string Description => "TemplateBinding: " + Property;

        public IBinding ProvideValue() => this;

        public InstancedBinding? Initiate(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            return new(target, InstanceCore(), Mode, BindingPriority.Template);
        }

        BindingExpressionBase IBinding2.Instance(AvaloniaObject target, AvaloniaProperty property, object? anchor)
        {
            return InstanceCore();
        }

        internal override bool WriteValueToSource(object? value)
        {
            if (Property is not null && TryGetTemplatedParent(out var templatedParent))
            {
                if (Converter is not null)
                    value = ConvertBack(Converter, ConverterCulture, ConverterParameter, value, TargetType);

                if (value != BindingOperations.DoNothing)
                    templatedParent.SetCurrentValue(Property, value);

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        void ISetterValue.Initialize(SetterBase setter) => _isSetterValue = true;

        protected override void StartCore()
        {
            OnTemplatedParentChanged();
            if (TryGetTarget(out var target))
                target.PropertyChanged += OnTargetPropertyChanged;
        }

        protected override void StopCore()
        {
            if (TryGetTarget(out var target))
            {
                if (target is StyledElement targetElement &&
                    targetElement?.TemplatedParent is { } templatedParent)
                {
                    templatedParent.PropertyChanged -= OnTemplatedParentPropertyChanged;
                }

                if (target is not null)
                {
                    target.PropertyChanged -= OnTargetPropertyChanged;
                }
            }
        }

        private object? ConvertToTargetType(object? value)
        {
            var converter = TargetTypeConverter.GetDefaultConverter();

            if (converter.TryConvert(value, TargetType, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            else
            {
                if (TryGetTarget(out var target))
                {
                    var valueString = value?.ToString() ?? "(null)";
                    var valueTypeName = value?.GetType().FullName ?? "null";
                    var message = $"Could not convert '{valueString}' ({valueTypeName}) to '{TargetType}'.";
                    Log(target, message, LogEventLevel.Warning);
                }

                return AvaloniaProperty.UnsetValue;
            }
        }

        private TemplateBinding InstanceCore()
        {
            if (Mode is BindingMode.OneTime or BindingMode.OneWayToSource)
                throw new NotSupportedException("TemplateBinding does not support OneTime or OneWayToSource bindings.");

            // Usually each `TemplateBinding` will only be instantiated once; in this case we can
            // use the `TemplateBinding` object itself as the binding expression in order to save
            // allocating a new object.
            //
            // If the binding appears in a `Setter`, then make a clone and instantiate that because
            // because the setter can outlive the control and cause a leak.
            if (!_isSetterValue)
            {
                return this;
            }
            else
            {
                var clone = new TemplateBinding
                {
                    Converter = Converter,
                    ConverterCulture = ConverterCulture,
                    ConverterParameter = ConverterParameter,
                    Mode = Mode,
                    Property = Property,
                };

                return clone;
            }
        }

        private void PublishValue()
        {
            if (Mode == BindingMode.OneWayToSource)
                return;

            if (TryGetTemplatedParent(out var templatedParent))
            {
                var value = Property is not null ?
                    templatedParent.GetValue(Property) :
                    templatedParent;
                BindingError? error = null;

                if (Converter is not null)
                    value = Convert(Converter, ConverterCulture, ConverterParameter, value, TargetType, ref error);

                value = ConvertToTargetType(value);
                PublishValue(value, error);

                if (Mode == BindingMode.OneTime)
                    Stop();
            }
            else
            {
                PublishValue(AvaloniaProperty.UnsetValue);
            }
        }

        private void OnTemplatedParentChanged()
        {
            if (TryGetTemplatedParent(out var templatedParent))
                templatedParent.PropertyChanged += OnTemplatedParentPropertyChanged;

            PublishValue();
        }

        private void OnTemplatedParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Property)
                PublishValue();
        }

        private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == StyledElement.TemplatedParentProperty)
            {
                if (e.OldValue is AvaloniaObject oldValue)
                    oldValue.PropertyChanged -= OnTemplatedParentPropertyChanged;

                OnTemplatedParentChanged();
            }
            else if (Mode is BindingMode.TwoWay or BindingMode.OneWayToSource && e.Property == TargetProperty)
            {
                WriteValueToSource(e.NewValue);
            }
        }

        private bool TryGetTemplatedParent([NotNullWhen(true)] out AvaloniaObject? result)
        {
            if (TryGetTarget(out var target) &&
                target is StyledElement targetElement &&
                targetElement.TemplatedParent is { } templatedParent)
            {
                result = templatedParent;
                return true;
            }

            result = null;
            return false;
        }
    }
}
