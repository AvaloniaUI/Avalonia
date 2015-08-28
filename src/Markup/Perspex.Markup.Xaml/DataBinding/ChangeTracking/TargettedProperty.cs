// -----------------------------------------------------------------------
// <copyright file="TargettedProperty.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.DataBinding.ChangeTracking
{
    using System;
    using System.Reflection;
    using Glass;

    internal class TargettedProperty
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
            get
            {
                return this.propertyInfo.GetValue(this.instance);
            }

            set
            {
                this.propertyInfo.SetValue(this.instance, value);
            }
        }

        public Type PropertyType => this.propertyInfo.PropertyType;

        public string Name => this.propertyInfo.Name;
    }
}