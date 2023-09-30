using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Avalonia.Data
{
    /// <summary>
    /// A XAML binding to a property on a control's templated parent.
    /// </summary>
    public class TemplateBinding : IObservable<object?>,
        IBinding,
        IDescription,
        IAvaloniaSubject<object?>,
        ISetterValue,
        IDisposable
    {
        private IObserver<object?>? _observer;
        private bool _isSetterValue;
        private StyledElement? _target;
        private Type? _targetType;
        private bool _hasProducedValue;

        public TemplateBinding()
        {
        }

        public TemplateBinding(AvaloniaProperty property)
        {
            Property = property;
        }

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            _ = observer ?? throw new ArgumentNullException(nameof(observer));
            Dispatcher.UIThread.VerifyAccess();

            if (_observer != null)
            {
                throw new InvalidOperationException("The observable can only be subscribed once.");
            }

            _observer = observer;
            Subscribed();

            return this;
        }

        public virtual void Dispose()
        {
            Unsubscribed();
            _observer = null;
        }

        /// <inheritdoc/>
        public InstancedBinding? Initiate(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            // Usually each `TemplateBinding` will only be instantiated once; in this case we can
            // use the `TemplateBinding` object itself as the instanced binding in order to save
            // allocating a new object.
            //
            // If the binding appears in a `Setter`, then make a clone and instantiate that because
            // because the setter can outlive the control and cause a leak.
            if (_target is null && !_isSetterValue)
            {
                _target = (StyledElement)target;
                _targetType = targetProperty?.PropertyType;

                return new InstancedBinding(
                    this,
                    Mode == BindingMode.Default ? BindingMode.OneWay : Mode,
                    BindingPriority.Template);
            }
            else
            {
                var clone = new TemplateBinding
                {
                    Converter = Converter,
                    ConverterParameter = ConverterParameter,
                    Property = Property,
                    Mode = Mode,
                };

                return clone.Initiate(target, targetProperty, anchor, enableDataValidation);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IValueConverter? Converter { get; set; }

        /// <summary>
        /// Gets or sets a parameter to pass to <see cref="Converter"/>.
        /// </summary>
        public object? ConverterParameter { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the name of the source property on the templated parent.
        /// </summary>
        public AvaloniaProperty? Property { get; set; }

        /// <inheritdoc/>
        public string Description => "TemplateBinding: " + Property;

        void IObserver<object?>.OnCompleted() => throw new NotImplementedException();
        void IObserver<object?>.OnError(Exception error) => throw new NotImplementedException();

        void IObserver<object?>.OnNext(object? value)
        {
            if (_target?.TemplatedParent is { } templatedParent && Property is not null)
            {
                if (Converter is not null)
                {
                    value = Converter.ConvertBack(
                        value,
                        Property.PropertyType,
                        ConverterParameter,
                        CultureInfo.CurrentCulture);
                }

                templatedParent.SetCurrentValue(Property, value);
            }
        }

        /// <inheritdoc/>
        void ISetterValue.Initialize(SetterBase setter) => _isSetterValue = true;

        private void Subscribed()
        {
            TemplatedParentChanged();

            if (_target is not null)
            {
                _target.PropertyChanged += TargetPropertyChanged;
            }
        }

        private void Unsubscribed()
        {
            if (_target?.TemplatedParent is { } templatedParent)
            {
                templatedParent.PropertyChanged -= TemplatedParentPropertyChanged;
            }

            if (_target is not null)
            {
                _target.PropertyChanged -= TargetPropertyChanged;
            }
        }

        private void PublishValue()
        {
            if (_target?.TemplatedParent is { } templatedParent)
            {
                var value = Property is not null ?
                    templatedParent.GetValue(Property) :
                    _target.TemplatedParent;

                if (Converter is not null)
                {
                    value = Converter.Convert(value, _targetType ?? typeof(object), ConverterParameter, CultureInfo.CurrentCulture);
                }

                _observer?.OnNext(value);
                _hasProducedValue = true;
            }
            else if (_hasProducedValue)
            {
                _observer?.OnNext(AvaloniaProperty.UnsetValue);
                _hasProducedValue = false;
            }
        }

        private void TemplatedParentChanged()
        {
            if (_target?.TemplatedParent is { } templatedParent)
            {
                templatedParent.PropertyChanged += TemplatedParentPropertyChanged;
            }

            PublishValue();
        }

        private void TargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == StyledElement.TemplatedParentProperty)
            {
                if (e.OldValue is AvaloniaObject oldValue)
                {
                    oldValue.PropertyChanged -= TemplatedParentPropertyChanged;
                }

                TemplatedParentChanged();
            }
        }

        private void TemplatedParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Property)
            {
                PublishValue();
            }
        }

        public IBinding ProvideValue() => this;
    }
}
