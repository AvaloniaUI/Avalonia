// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Markup.Xaml.Data;
using OmniXaml;
using OmniXaml.Typing;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexXamlMember : XamlMember
    {
        public PerspexXamlMember(string name,
            XamlType owner,
            IXamlTypeRepository xamlTypeRepository,
            ITypeFeatureProvider featureProvider)
            : base(name, owner, xamlTypeRepository, featureProvider)
        {
        }

        protected override IXamlMemberValuePlugin LookupXamlMemberValueConnector()
        {
            return new PerspexXamlMemberValuePlugin(this);
        }

        public override string ToString()
        {
            return "Perspex XAML Member " + base.ToString();
        }
    }
}