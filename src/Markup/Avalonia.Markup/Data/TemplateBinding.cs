using System;
using System.Globalization;
using System.Reactive.Subjects;
using Avalonia.Data.Converters;
using Avalonia.Reactive;

namespace Avalonia.Data
{
    /// <summary>
    /// A XAML binding to a property on a control's templated parent.
    /// </summary>
    public class TemplateBinding : SingleSubscriberObservableBase<object>,
        IBinding,
        IDescription,
        ISubject<object>
    {
        private IStyledElement _target;
        private Type _targetType;

        public TemplateBinding()
        {
        }

        public TemplateBinding(AvaloniaProperty property)
        {
            Property = property;
        }

        /// <inheritdoc/>
        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor = null,
            bool enableDataValidation = false)
        {
            // Usually each `TemplateBinding` will only be instantiated once; in this case we can
            // use the `TemplateBinding` object itself as the instanced binding in order to save
            // allocating a new object. If the binding *is* instantiated more than once (which can
            // happen if it appears in a `Setter` for example, then just make a clone and instantiate
            // that.
            if (_target == null)
            {
                _target = (IStyledElement)target;
                _targetType = targetProperty?.PropertyType;

                return new InstancedBinding(
                    this,
                    Mode == BindingMode.Default ? BindingMode.OneWay : Mode,
                    BindingPriority.TemplatedParent);
            }
            else
            {
                var clone = new TemplateBinding
                {
                    Converter = Converter,
                    ConverterParameter = ConverterParameter,
                    Property = Property,
                };

                return clone.Initiate(target, targetProperty, anchor, enableDataValidation);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IValueConverter Converter { get; set; }

        /// <summary>
        /// Gets or sets a parameter to pass to <see cref="Converter"/>.
        /// </summary>
        public object ConverterParameter { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the name of the source property on the templated parent.
        /// </summary>
        public AvaloniaProperty Property { get; set; }

        /// <inheritdoc/>
        public string Description => "TemplateBinding: " + Property;

        void IObserver<object>.OnCompleted() => throw new NotImplementedException();
        void IObserver<object>.OnError(Exception error) => throw new NotImplementedException();

        void IObserver<object>.OnNext(object value)
        {
            if (_target.TemplatedParent != null && Property != null)
            {
                if (Converter != null)
                {
                    value = Converter.ConvertBack(
                        value,
                        Property.PropertyType,
                        ConverterParameter,
                        CultureInfo.CurrentCulture);
                }

                // Use LocalValue priority here, as TemplatedParent doesn't make sense on controls
                // that aren't template children.
                _target.TemplatedParent.SetValue(Property, value, BindingPriority.LocalValue);
            }
        }

        protected override void Subscribed()
        {
            TemplatedParentChanged();
            _target.PropertyChanged += TargetPropertyChanged;
        }

        protected override void Unsubscribed()
        {
            if (_target.TemplatedParent != null)
            {
                _target.TemplatedParent.PropertyChanged -= TemplatedParentPropertyChanged;
            }

            _target.PropertyChanged -= TargetPropertyChanged;
        }

        private void PublishValue()
        {
            if (_target.TemplatedParent != null)
            {
                var value = Property != null ?
                    _target.TemplatedParent.GetValue(Property) :
                    _target.TemplatedParent;

                if (Converter != null)
                {
                    value = Converter.Convert(value, _targetType, ConverterParameter, CultureInfo.CurrentCulture);
                }

                PublishNext(value);
            }
            else
            {
                PublishNext(AvaloniaProperty.UnsetValue);
            }
        }

        private void TemplatedParentChanged()
        {
            if (_target.TemplatedParent != null)
            {
                _target.TemplatedParent.PropertyChanged += TemplatedParentPropertyChanged;
            }

            PublishValue();
        }

        private void TargetPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == StyledElement.TemplatedParentProperty)
            {
                var oldValue = (IAvaloniaObject)e.OldValue;
                var newValue = (IAvaloniaObject)e.OldValue;

                if (oldValue != null)
                {
                    oldValue.PropertyChanged -= TemplatedParentPropertyChanged;
                }

                TemplatedParentChanged();
            }
        }

        private void TemplatedParentPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Property)
            {
                PublishValue();
            }
        }
    }
}
