// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Markup.Xaml.DataBinding;
using OmniXaml;
using OmniXaml.Typing;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexXamlMember : XamlMember
    {
        private readonly IPerspexPropertyBinder _propertyBinder;

        public PerspexXamlMember(string name,
            XamlType owner,
            IXamlTypeRepository xamlTypeRepository,
            ITypeFeatureProvider featureProvider,
            IPerspexPropertyBinder propertyBinder)
            : base(name, owner, xamlTypeRepository, featureProvider)
        {
            _propertyBinder = propertyBinder;
        }

        protected override IXamlMemberValuePlugin LookupXamlMemberValueConnector()
        {
            return new PerspexXamlMemberValuePlugin(this, _propertyBinder);
        }

        public override string ToString()
        {
            return "Perspex XAML Member " + base.ToString();
        }
    }
}