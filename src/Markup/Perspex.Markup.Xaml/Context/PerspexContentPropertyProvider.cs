// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Glass;
using OmniXaml;
using OmniXaml.Builder;
using Perspex.Metadata;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexContentPropertyProvider : IContentPropertyProvider
    {
        private readonly Dictionary<Type, string> _values = new Dictionary<Type, string>();

        public string GetContentPropertyName(Type type)
        {
            string result;

            if (!_values.TryGetValue(type, out result))
            {
                result = LookupContentProperty(type);
                _values[type] = result;
            }

            return result;
        }

        private string LookupContentProperty(Type type)
        {
            var result = (from member in type.GetRuntimeProperties()
                          let att = member.GetCustomAttribute<ContentAttribute>()
                          where att != null
                          select member).FirstOrDefault();

            return result?.Name;
        }

        void IAdd<ContentPropertyDefinition>.Add(ContentPropertyDefinition item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator<ContentPropertyDefinition> IEnumerable<ContentPropertyDefinition>.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
