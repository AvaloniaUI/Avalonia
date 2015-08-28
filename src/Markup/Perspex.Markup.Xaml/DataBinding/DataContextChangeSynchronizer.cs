// -----------------------------------------------------------------------
// <copyright file="DataContextChangeSynchronizer.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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

            this.bindingEndpoint = new TargetBindingEndpoint(target, targetProperty);
            this.sourceEndpoint = new ObservablePropertyBranch(source, sourcePropertyPath);
            this.targetPropertyTypeConverter = typeConverterProvider.GetTypeConverter(targetProperty.PropertyType);
        }

        private bool CanAssignWithoutConversion
        {
            get
            {
                var sourceTypeInfo = this.sourceEndpoint.Type.GetTypeInfo();
                var targetTypeInfo = this.bindingEndpoint.Property.PropertyType.GetTypeInfo();
                var compatible = targetTypeInfo.IsAssignableFrom(sourceTypeInfo);
                return compatible;
            }
        }

        public void SubscribeModelToUI()
        {
            this.bindingEndpoint.Object.GetObservable(this.bindingEndpoint.Property).Subscribe(this.UpdateModelFromUI);
        }

        public void SubscribeUIToModel()
        {
            this.sourceEndpoint.Changed.Subscribe(_ => this.UpdateUIFromModel());
            this.UpdateUIFromModel();
        }

        private void UpdateUIFromModel()
        {
            object contextGetter = this.sourceEndpoint.Value;
            this.SetCompatibleValue(contextGetter, this.bindingEndpoint.Property.PropertyType, o => this.bindingEndpoint.Object.SetValue(this.bindingEndpoint.Property, o));
        }

        private void SetCompatibleValue(object originalValue, Type targetType, Action<object> setValueFunc)
        {
            if (originalValue == null)
            {
                setValueFunc(null);
            }
            else
            {
                if (this.CanAssignWithoutConversion)
                {
                    setValueFunc(originalValue);
                }
                else
                {
                    var synchronizationOk = false;

                    if (this.targetPropertyTypeConverter != null)
                    {
                        if (this.targetPropertyTypeConverter.CanConvertTo(null, targetType))
                        {
                            object convertedValue = this.targetPropertyTypeConverter.ConvertTo(null, CultureInfo.InvariantCulture, originalValue,
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
                        this.LogCannotConvertError(originalValue);
                    }
                }
            }
        }

        private void UpdateModelFromUI(object valueFromUI)
        {
            this.SetCompatibleValue(valueFromUI, this.sourceEndpoint.Type, o => this.sourceEndpoint.Value = o);
        }

        private void LogCannotConvertError(object value)
        {
            Contract.Requires<ArgumentException>(value != null);

            var loggableValue = value.ToString();
            var valueToWrite = string.IsNullOrWhiteSpace(loggableValue) ? "'(empty/whitespace string)'" : loggableValue;

            Debug.WriteLine("Cannot convert value {0} ({1}) to {2}", valueToWrite, value.GetType(), this.bindingEndpoint.Property.PropertyType);
        }
    }
}