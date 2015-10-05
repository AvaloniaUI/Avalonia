// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Glass;
using OmniXaml;
using OmniXaml.Typing;
using Perspex.Markup.Xaml.Binding;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexTypeRepository : XamlTypeRepository
    {
        private readonly ITypeFactory _typeFactory;

        public PerspexTypeRepository(IXamlNamespaceRegistry xamlNamespaceRegistry,
            ITypeFactory typeFactory,
            ITypeFeatureProvider featureProvider) : base(xamlNamespaceRegistry, typeFactory, featureProvider)
        {
            _typeFactory = typeFactory;
        }

        public override XamlType GetXamlType(Type type)
        {
            Guard.ThrowIfNull(type, nameof(type));
            return new PerspexXamlType(type, this, _typeFactory, FeatureProvider);
        }
    }
}