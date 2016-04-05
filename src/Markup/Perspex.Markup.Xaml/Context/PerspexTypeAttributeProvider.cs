// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using Perspex.Metadata;
using Portable.Xaml.ComponentModel;
using Portable.Xaml.Markup;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexTypeAttributeProvider : ICustomAttributeProvider
    {
        private readonly Type _type;

        public PerspexTypeAttributeProvider(Type type)
        {
            _type = type;
        }

        public object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == typeof(Portable.Xaml.Markup.ContentPropertyAttribute))
            {
                var cp = GetContentPropertyAttribute(inherit);

                if (cp != null)
                {
                    return new object[] { cp };
                }
            }
            
            var attr = _type.GetTypeInfo().GetCustomAttributes(attributeType, inherit);
            return (attr as object[]) ?? attr.ToArray();
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        private Attribute GetContentPropertyAttribute(bool inherit)
        {
            var type = _type;

            while (type != null)
            {
                var properties = type.GetTypeInfo().DeclaredProperties
                    .Where(x => x.GetCustomAttribute<ContentAttribute>() != null);
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
                    return new ContentPropertyAttribute(result);
                }

                type = inherit ? type.GetTypeInfo().BaseType : null;
            }

            return null;
        }
    }
}
