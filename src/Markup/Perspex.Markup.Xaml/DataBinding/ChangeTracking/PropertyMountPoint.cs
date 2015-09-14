// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using Glass;

namespace Perspex.Markup.Xaml.DataBinding.ChangeTracking
{
    public class PropertyMountPoint
    {
        private readonly TargettedProperty _referencedTargettedProperty;

        public PropertyMountPoint(object origin, PropertyPath propertyPath)
        {
            Guard.ThrowIfNull(origin, nameof(origin));
            Guard.ThrowIfNull(propertyPath, nameof(propertyPath));

            _referencedTargettedProperty = GetReferencedPropertyInfo(origin, propertyPath, 0);
        }

        private static TargettedProperty GetReferencedPropertyInfo(object current, PropertyPath propertyPath, int level)
        {
            var typeInfo = current.GetType().GetTypeInfo();
            var leftPropertyInfo = typeInfo.GetDeclaredProperty(propertyPath.Chunks[level]);

            if (level == propertyPath.Chunks.Length - 1)
            {
                return new TargettedProperty(current, leftPropertyInfo);
            }

            var nextInstance = leftPropertyInfo.GetValue(current);

            return GetReferencedPropertyInfo(nextInstance, propertyPath, level + 1);
        }

        public object Value
        {
            get
            {
                return _referencedTargettedProperty.Value;
            }

            set
            {
                _referencedTargettedProperty.Value = value;
            }
        }

        public Type ProperyType => _referencedTargettedProperty.PropertyType;
    }
}