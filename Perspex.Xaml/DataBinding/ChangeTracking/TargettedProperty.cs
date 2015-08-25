namespace Perspex.Xaml.DataBinding.ChangeTracking
{
    using System;
    using System.Reflection;
    using Controls;
    using Glass;

    class TargettedProperty
    {
        private readonly object instance;
        private readonly PropertyInfo propertyInfo;

        public TargettedProperty(object instance, PropertyInfo propertyInfo)
        {
            Guard.ThrowIfNull(instance, nameof(instance));
            Guard.ThrowIfNull(propertyInfo, nameof(propertyInfo));

            this.instance = instance;
            this.propertyInfo = propertyInfo;
        }

        public object Value
        {
            get { return propertyInfo.GetValue(instance); }
            set
            {
                propertyInfo.SetValue(instance, value);
            }
        }

        public Type PropertyType => propertyInfo.PropertyType;
        public string Name => propertyInfo.Name;
    }
}