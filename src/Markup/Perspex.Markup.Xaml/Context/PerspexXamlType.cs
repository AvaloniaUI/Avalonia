// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml;
using OmniXaml.Typing;
using Perspex.Markup.Xaml.DataBinding;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexXamlType : XamlType
    {
        private readonly IPerspexPropertyBinder _propertyBinder;

        public PerspexXamlType(Type type,
            IXamlTypeRepository typeRepository,
            ITypeFactory typeFactory,
            ITypeFeatureProvider featureProvider,
            IPerspexPropertyBinder propertyBinder) : base(type, typeRepository, typeFactory, featureProvider)
        {
            _propertyBinder = propertyBinder;
        }

        protected IPerspexPropertyBinder PropertyBinder => _propertyBinder;

        protected override XamlMember LookupMember(string name)
        {
            return new PerspexXamlMember(name, this, TypeRepository, FeatureProvider, _propertyBinder);
        }

        public override string ToString()
        {
            return "Perspex XAML Type " + base.ToString();
        }
    }
}