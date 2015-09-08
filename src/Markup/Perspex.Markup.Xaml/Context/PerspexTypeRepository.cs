// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Glass;
using OmniXaml;
using OmniXaml.Typing;
using Perspex.Markup.Xaml.DataBinding;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexTypeRepository : XamlTypeRepository
    {
        private readonly ITypeFactory _typeFactory;
        private readonly IPerspexPropertyBinder _propertyBinder;

        public PerspexTypeRepository(IXamlNamespaceRegistry xamlNamespaceRegistry,
            ITypeFactory typeFactory,
            ITypeFeatureProvider featureProvider,
            IPerspexPropertyBinder propertyBinder) : base(xamlNamespaceRegistry, typeFactory, featureProvider)
        {
            _typeFactory = typeFactory;
            _propertyBinder = propertyBinder;
        }

        public override XamlType GetXamlType(Type type)
        {
            Guard.ThrowIfNull(type, nameof(type));
            return new PerspexXamlType(type, this, _typeFactory, FeatureProvider, _propertyBinder);
        }
    }
}