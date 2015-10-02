// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml;
using OmniXaml.Typing;
using Perspex.Markup.Xaml.Binding;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexXamlType : XamlType
    {
        public PerspexXamlType(Type type,
            IXamlTypeRepository typeRepository,
            ITypeFactory typeFactory,
            ITypeFeatureProvider featureProvider) : base(type, typeRepository, typeFactory, featureProvider)
        {
        }

        protected override XamlMember LookupMember(string name)
        {
            return new PerspexXamlMember(name, this, TypeRepository, FeatureProvider);
        }

        public override string ToString()
        {
            return "Perspex XAML Type " + base.ToString();
        }
    }
}