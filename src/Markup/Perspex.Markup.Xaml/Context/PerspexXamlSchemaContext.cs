// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexXamlSchemaContext : XamlSchemaContext
    {
        private readonly Dictionary<Type, XamlType> s_typeCache = new Dictionary<Type, XamlType>();

        protected override ICustomAttributeProvider GetCustomAttributeProvider(Type type)
        {
            return new PerspexTypeAttributeProvider(type);
        }

        protected override ICustomAttributeProvider GetCustomAttributeProvider(MemberInfo member)
        {
            return new PerspexMemberAttributeProvider(member);
        }

        public override XamlType GetXamlType(Type type)
        {
            XamlType xamlType;

            if (!s_typeCache.TryGetValue(type, out xamlType))
            {
                xamlType = new PerspexXamlType(type, this);
                s_typeCache.Add(type, xamlType);
            }

            return xamlType;
        }
    }
}
