// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using Portable.Xaml.ComponentModel;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexMemberAttributeProvider : ICustomAttributeProvider
    {
        readonly MemberInfo _info;

        public PerspexMemberAttributeProvider(MemberInfo info)
        {
            this._info = info;
        }

        public object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            object[] result = null;

            if (attributeType == typeof(Portable.Xaml.Markup.AmbientAttribute))
            {
                var attr = _info.GetCustomAttributes(typeof(Perspex.Metadata.AmbientAttribute), inherit);
                result = (attr as object[]) ?? attr.ToArray();
            }

            if (result == null || result.Length == 0)
            {
                var attr = _info.GetCustomAttributes(attributeType, inherit);
                return (attr as object[]) ?? attr.ToArray();
            }
            else
            {
                return result;
            }
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}
