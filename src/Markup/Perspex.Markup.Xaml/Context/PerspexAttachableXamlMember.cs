// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Markup.Xaml.Data;
using OmniXaml;
using OmniXaml.Typing;
using System.Reflection;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexAttachableXamlMember : AttachableXamlMember
    {
        public PerspexAttachableXamlMember(string name,
            XamlType owner,
            MethodInfo getter,
            MethodInfo setter,
            IXamlTypeRepository xamlTypeRepository,
            ITypeFeatureProvider featureProvider)
            : base(name, getter, setter, xamlTypeRepository, featureProvider)
        {
        }

        protected override IXamlMemberValuePlugin LookupXamlMemberValueConnector()
        {
            return new PerspexXamlMemberValuePlugin(this);
        }

        public override string ToString()
        {
            return "Perspex Attachable XAML Member " + base.ToString();
        }
    }
}