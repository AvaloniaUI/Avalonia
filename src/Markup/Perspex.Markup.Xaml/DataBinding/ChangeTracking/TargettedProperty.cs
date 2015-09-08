// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using Glass;

namespace Perspex.Markup.Xaml.DataBinding.ChangeTracking
{
    internal class TargettedProperty
    {
        private readonly object _instance;
        private readonly PropertyInfo _propertyInfo;

        public TargettedProperty(object instance, PropertyInfo propertyInfo)
        {
            Guard.ThrowIfNull(instance, nameof(instance));
            Guard.ThrowIfNull(propertyInfo, nameof(propertyInfo));

            _instance = instance;
            _propertyInfo = propertyInfo;
        }

        public object Value
        {
            get
            {
                return _propertyInfo.GetValue(_instance);
            }

            set
            {
                _propertyInfo.SetValue(_instance, value);
            }
        }

        public Type PropertyType => _propertyInfo.PropertyType;

        public string Name => _propertyInfo.Name;
    }
}