// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Portable.Xaml.ComponentModel;
using System.ComponentModel;
using System;
using System.Linq;
using System.Reflection;
using avm = Avalonia.Metadata;
using pm = Portable.Xaml.Markup;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    internal class AvaloniaTypeAttributeProvider : ICustomAttributeProvider
    {
        public AvaloniaTypeAttributeProvider(Type type)
        {
            _type = type;
        }

        public object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            Attribute result = null;

            var ti = _type.GetTypeInfo();

            if (attributeType == typeof(pm.ContentPropertyAttribute))
            {
                result = GetContentPropertyAttribute(inherit);
            }
            else if (attributeType == typeof(pm.RuntimeNamePropertyAttribute))
            {
                if (_namedType.IsAssignableFrom(ti))
                {
                    result = new pm.RuntimeNamePropertyAttribute(nameof(INamed.Name));
                }
            }
            else if (attributeType == typeof(TypeConverterAttribute))
            {
                result = ti.GetCustomAttribute(attributeType, inherit);

                if (result == null)
                {
                    var convType = AvaloniaDefaultTypeConverters.GetTypeConverter(_type);

                    if (convType != null)
                    {
                        result = new TypeConverterAttribute(convType);
                    }
                }
            }
            else if (attributeType == typeof(pm.AmbientAttribute))
            {
                result = ti.GetCustomAttribute<avm.AmbientAttribute>(inherit)
                                                    .ToPortableXaml();
            }

            if (result == null)
            {
                var attr = ti.GetCustomAttributes(attributeType, inherit);
                return (attr as object[]) ?? attr.ToArray();
            }
            else
            {
                return new object[] { result };
            }
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        private readonly TypeInfo _namedType = typeof(INamed).GetTypeInfo();

        private readonly Type _type;

        private Attribute GetContentPropertyAttribute(bool inherit)
        {
            var type = _type;

            while (type != null)
            {
                var properties = type.GetTypeInfo().DeclaredProperties
                    .Where(x => x.GetCustomAttribute<avm.ContentAttribute>() != null);
                string result = null;

                foreach (var property in properties)
                {
                    if (result != null)
                    {
                        throw new Exception($"Content property defined more than once on {type}.");
                    }

                    result = property.Name;
                }

                if (result != null)
                {
                    return new pm.ContentPropertyAttribute(result);
                }

                type = inherit ? type.GetTypeInfo().BaseType : null;
            }

            return null;
        }
    }
}