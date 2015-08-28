namespace Perspex.Markup.Xaml.DataBinding
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using ChangeTracking;
    using Glass;
    using OmniXaml.TypeConversion;

    public class DataContextChangeSynchronizer
    {
        private readonly ITypeConverter targetPropertyTypeConverter;
        private readonly TargetBindingEndpoint bindingEndpoint;
        private readonly ObservablePropertyBranch sourceEndpoint;

        public DataContextChangeSynchronizer(PerspexObject target, PerspexProperty targetProperty,
            PropertyPath sourcePropertyPath, object source, ITypeConverterProvider typeConverterProvider)
        {
            Guard.ThrowIfNull(target, nameof(target));
            Guard.ThrowIfNull(targetProperty, nameof(targetProperty));
            Guard.ThrowIfNull(sourcePropertyPath, nameof(sourcePropertyPath));
            Guard.ThrowIfNull(source, nameof(source));
            Guard.ThrowIfNull(typeConverterProvider, nameof(typeConverterProvider));

            bindingEndpoint = new TargetBindingEndpoint(target, targetProperty);
            sourceEndpoint = new ObservablePropertyBranch(source, sourcePropertyPath);
            targetPropertyTypeConverter = typeConverterProvider.GetTypeConverter(targetProperty.PropertyType);
        }

        private bool CanAssignWithoutConversion
        {
            get
            {
                var sourceTypeInfo = sourceEndpoint.Type.GetTypeInfo();
                var targetTypeInfo = bindingEndpoint.Property.PropertyType.GetTypeInfo();
                var compatible = targetTypeInfo.IsAssignableFrom(sourceTypeInfo);
                return compatible;
            }
        }

        public void SubscribeModelToUI()
        {
            bindingEndpoint.Object.GetObservable(bindingEndpoint.Property).Subscribe(UpdateModelFromUI);
        } 

        public void SubscribeUIToModel()
        {
            sourceEndpoint.Changed.Subscribe(_ => UpdateUIFromModel());
            UpdateUIFromModel();
        }

        private void UpdateUIFromModel()
        {
            object contextGetter = sourceEndpoint.Value;
            SetCompatibleValue(contextGetter, bindingEndpoint.Property.PropertyType, o => bindingEndpoint.Object.SetValue(bindingEndpoint.Property, o));
        }

        private void SetCompatibleValue(object originalValue, Type targetType, Action<object> setValueFunc)
        {
            if (originalValue == null)
            {
                setValueFunc(null);
            }
            else
            {
                if (CanAssignWithoutConversion)
                {
                    setValueFunc(originalValue);
                }
                else
                {
                    var synchronizationOk = false;

                    if (targetPropertyTypeConverter != null)
                    {
                        if (targetPropertyTypeConverter.CanConvertTo(null, targetType))
                        {
                            object convertedValue = targetPropertyTypeConverter.ConvertTo(null, CultureInfo.InvariantCulture, originalValue,
                                targetType);

                            if (convertedValue != null)
                            {
                                setValueFunc(convertedValue);
                                synchronizationOk = true;
                            }
                        }
                    }

                    if (!synchronizationOk)
                    {
                        LogCannotConvertError(originalValue);
                    }
                }
            }
        }

        private void UpdateModelFromUI(object valueFromUI)
        {
            SetCompatibleValue(valueFromUI, sourceEndpoint.Type, o => sourceEndpoint.Value = o);
        }

        private void LogCannotConvertError(object value)
        {
            Contract.Requires<ArgumentException>(value != null);

            var loggableValue = value.ToString();
            var valueToWrite = string.IsNullOrWhiteSpace(loggableValue) ? "'(empty/whitespace string)'" : loggableValue;

            Debug.WriteLine("Cannot convert value {0} ({1}) to {2}", valueToWrite, value.GetType(), bindingEndpoint.Property.PropertyType);
        }        
    }
}