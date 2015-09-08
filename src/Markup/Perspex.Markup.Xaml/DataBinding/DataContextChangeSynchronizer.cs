





namespace Perspex.Markup.Xaml.DataBinding
{
    using System;
    using System.Globalization;
    using System.Reactive.Linq;
    using System.Reflection;
    using ChangeTracking;
    using Glass;
    using OmniXaml.TypeConversion;

    public class DataContextChangeSynchronizer
    {
        private readonly BindingTarget bindingTarget;
        private readonly ITypeConverter targetPropertyTypeConverter;
        private readonly TargetBindingEndpoint bindingEndpoint;
        private readonly ObservablePropertyBranch sourceEndpoint;

        public DataContextChangeSynchronizer(BindingSource bindingSource, BindingTarget bindingTarget, ITypeConverterProvider typeConverterProvider)
        {
            this.bindingTarget = bindingTarget;
            Guard.ThrowIfNull(bindingTarget.Object, nameof(bindingTarget.Object));
            Guard.ThrowIfNull(bindingTarget.Property, nameof(bindingTarget.Property));
            Guard.ThrowIfNull(bindingSource.SourcePropertyPath, nameof(bindingSource.SourcePropertyPath));
            Guard.ThrowIfNull(bindingSource.Source, nameof(bindingSource.Source));
            Guard.ThrowIfNull(typeConverterProvider, nameof(typeConverterProvider));

            this.bindingEndpoint = new TargetBindingEndpoint(bindingTarget.Object, bindingTarget.Property);
            this.sourceEndpoint = new ObservablePropertyBranch(bindingSource.Source, bindingSource.SourcePropertyPath);
            this.targetPropertyTypeConverter = typeConverterProvider.GetTypeConverter(bindingTarget.Property.PropertyType);
        }

        public class BindingTarget
        {
            private readonly PerspexObject obj;
            private readonly PerspexProperty property;

            public BindingTarget(PerspexObject @object, PerspexProperty property)
            {
                this.obj = @object;
                this.property = property;
            }

            public PerspexObject Object => obj;

            public PerspexProperty Property => property;

            public object Value
            {
                get { return obj.GetValue(property); }
                set { obj.SetValue(property, value); }
            }
        }

        public class BindingSource
        {
            private readonly PropertyPath sourcePropertyPath;
            private readonly object source;

            public BindingSource(PropertyPath sourcePropertyPath, object source)
            {
                this.sourcePropertyPath = sourcePropertyPath;
                this.source = source;
            }

            public PropertyPath SourcePropertyPath => this.sourcePropertyPath;

            public object Source => source;
        }

        public void StartUpdatingTargetWhenSourceChanges()
        {
            // TODO: commenting out this line will make the existing value to be skipped from the SourceValues. This is not supposed to happen. Is it?
            bindingTarget.Value = ConvertedValue(sourceEndpoint.Value, bindingTarget.Property.PropertyType);

            // We use the native Bind method from PerspexObject to subscribe to the SourceValues observable
            this.bindingTarget.Object.Bind(this.bindingTarget.Property, this.SourceValues);
        }

        public void StartUpdatingSourceWhenTargetChanges()
        {
            // We subscribe to the TargetValues and each time we have a new value, we update the source with it
            this.TargetValues.Subscribe(newValue => this.sourceEndpoint.Value = newValue);
        }

        private IObservable<object> SourceValues
        {
            get
            {
                return this.sourceEndpoint.Values.Select(originalValue => this.ConvertedValue(originalValue, this.bindingTarget.Property.PropertyType));
            }
        }

        private IObservable<object> TargetValues
        {
            get
            {
                return this.bindingEndpoint.Object
                    .GetObservable(this.bindingEndpoint.Property).Select(o => this.ConvertedValue(o, this.sourceEndpoint.Type));
            }
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

        private object ConvertedValue(object originalValue, Type propertyType)
        {
            object converted;
            if (this.TryConvert(originalValue, propertyType, out converted))
            {
                return converted;
            }

            return null;
        }

        private bool TryConvert(object originalValue, Type targetType, out object finalValue)
        {
            if (originalValue != null)
            {
                if (this.CanAssignWithoutConversion)
                {
                    finalValue = originalValue;
                    return true;
                }

                if (this.targetPropertyTypeConverter != null)
                {
                    if (this.targetPropertyTypeConverter.CanConvertTo(null, targetType))
                    {
                        object convertedValue = this.targetPropertyTypeConverter.ConvertTo(
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