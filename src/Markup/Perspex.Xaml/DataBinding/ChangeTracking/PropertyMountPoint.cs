﻿namespace Perspex.Xaml.DataBinding.ChangeTracking
{
    using System;
    using System.Reflection;
    using Glass;

    public class PropertyMountPoint
    {
        private readonly TargettedProperty referencedTargettedProperty;

        public PropertyMountPoint(object origin, PropertyPath propertyPath)
        {   
            Guard.ThrowIfNull(origin, nameof(origin));
            Guard.ThrowIfNull(propertyPath, nameof(propertyPath));

            this.referencedTargettedProperty = GetReferencedPropertyInfo(origin, propertyPath, 0);
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
                return referencedTargettedProperty.Value;
            }
            set
            {
                referencedTargettedProperty.Value = value;
            }
        }

        public Type ProperyType => referencedTargettedProperty.PropertyType;
    }
}