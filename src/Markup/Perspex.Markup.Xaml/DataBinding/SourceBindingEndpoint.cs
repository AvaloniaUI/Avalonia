





namespace Perspex.Markup.Xaml.DataBinding
{
    using System;
    using System.ComponentModel;

    public class SourceBindingEndpoint
    {
        public Type PropertyType { get; }

        public INotifyPropertyChanged Source { get; }

        public dynamic PropertyGetter { get; }

        public Delegate PropertySetter { get; }

        public SourceBindingEndpoint(INotifyPropertyChanged source, Type propertyType, dynamic propertyGetter, Delegate propertySetter)
        {
            this.Source = source;
            this.PropertyType = propertyType;
            this.PropertyGetter = propertyGetter;
            this.PropertySetter = propertySetter;
        }
    }
}