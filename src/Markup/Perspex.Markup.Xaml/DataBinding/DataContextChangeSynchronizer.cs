// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Reflection;
using Glass;
using OmniXaml.TypeConversion;
using Perspex.Markup.Xaml.DataBinding.ChangeTracking;

namespace Perspex.Markup.Xaml.DataBinding
{
    public class DataContextChangeSynchronizer
    {
        private readonly BindingTarget _bindingTarget;
        private readonly ITypeConverter _targetPropertyTypeConverter;
        private readonly TargetBindingEndpoint _bindingEndpoint;
        private readonly ObservablePropertyBranch _sourceEndpoint;

        public DataContextChangeSynchronizer(BindingSource bindingSource, BindingTarget bindingTarget, ITypeConverterProvider typeConverterProvider)
        {
            _bindingTarget = bindingTarget;
            Guard.ThrowIfNull(bindingTarget.Object, nameof(bindingTarget.Object));
            Guard.ThrowIfNull(bindingTarget.Property, nameof(bindingTarget.Property));
            Guard.ThrowIfNull(bindingSource.SourcePropertyPath, nameof(bindingSource.SourcePropertyPath));
            Guard.ThrowIfNull(bindingSource.Source, nameof(bindingSource.Source));
            Guard.ThrowIfNull(typeConverterProvider, nameof(typeConverterProvider));

            _bindingEndpoint = new TargetBindingEndpoint(bindingTarget.Object, bindingTarget.Property);
            _sourceEndpoint = new ObservablePropertyBranch(bindingSource.Source, bindingSource.SourcePropertyPath);
            _targetPropertyTypeConverter = typeConverterProvider.GetTypeConverter(bindingTarget.Property.PropertyType);
        }

        public class BindingTarget
        {
            private readonly PerspexObject _obj;
            private readonly PerspexProperty _property;

            public BindingTarget(PerspexObject @object, PerspexProperty property)
            {
                _obj = @object;
                _property = property;
            }

            public PerspexObject Object => _obj;

            public PerspexProperty Property => _property;

            public object Value
            {
                get { return _obj.GetValue(_property); }
                set { _obj.SetValue(_property, value); }
            }
        }

        public class BindingSource
        {
            private readonly PropertyPath _sourcePropertyPath;
            private readonly object _source;

            public BindingSource(PropertyPath sourcePropertyPath, object source)
            {
                _sourcePropertyPath = sourcePropertyPath;
                _source = source;
            }

            public PropertyPath SourcePropertyPath => _sourcePropertyPath;

            public object Source => _source;
        }

        public void StartUpdatingTargetWhenSourceChanges()
        {
            // TODO: commenting out this line will make the existing value to be skipped from the SourceValues. This is not supposed to happen. Is it?
            _bindingTarget.Value = ConvertedValue(_sourceEndpoint.Value, _bindingTarget.Property.PropertyType);

            // We use the native Bind method from PerspexObject to subscribe to the SourceValues observable
            _bindingTarget.Object.Bind(_bindingTarget.Property, SourceValues);
        }

        public void StartUpdatingSourceWhenTargetChanges()
        {
            // We subscribe to the TargetValues and each time we have a new value, we update the source with it
            TargetValues.Subscribe(newValue => _sourceEndpoint.Value = newValue);
        }

        private IObservable<object> SourceValues
        {
            get
            {
                return _sourceEndpoint.Values.Select(originalValue => ConvertedValue(originalValue, _bindingTarget.Property.PropertyType));
            }
        }

        private IObservable<object> TargetValues
        {
            get
            {
                return _bindingEndpoint.Object
                    .GetObservable(_bindingEndpoint.Property).Select(o => ConvertedValue(o, _sourceEndpoint.Type));
            }
        }

        private bool CanAssignWithoutConversion
        {
            get
            {
                var sourceTypeInfo = _sourceEndpoint.Type.GetTypeInfo();
                var targetTypeInfo = _bindingEndpoint.Property.PropertyType.GetTypeInfo();
                var compatible = targetTypeInfo.IsAssignableFrom(sourceTypeInfo);
                return compatible;
            }
        }

        private object ConvertedValue(object originalValue, Type propertyType)
        {
            object converted;
            if (TryConvert(originalValue, propertyType, out converted))
            {
                return converted;
            }

            return null;
        }

        private bool TryConvert(object originalValue, Type targetType, out object finalValue)
        {
            if (originalValue != null)
            {
                if (CanAssignWithoutConversion)
                {
                    finalValue = originalValue;
                    return true;
                }

                if (_targetPropertyTypeConverter != null)
                {
                    if (_targetPropertyTypeConverter.CanConvertTo(null, targetType))
                    {
                        object convertedValue = _targetPropertyTypeConverter.ConvertTo(
                            null,
                            CultureInfo.InvariantCulture,
                            originalValue,
                            targetType);

                        if (convertedValue != null)
                        {
                            finalValue = convertedValue;
                            return true;
                        }
                    }
                }
            }
            else
            {
                finalValue = null;
                return true;
            }

            finalValue = null;
            return false;
        }
    }
}