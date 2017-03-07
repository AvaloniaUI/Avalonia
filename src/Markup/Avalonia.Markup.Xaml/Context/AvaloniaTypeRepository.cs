// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml;
using OmniXaml.Typing;
using Glass.Core;

namespace Avalonia.Markup.Xaml.Context
{
#if OMNIXAML
    public class AvaloniaTypeRepository : TypeRepository
    {
        private readonly ITypeFactory _typeFactory;

        public AvaloniaTypeRepository(INamespaceRegistry xamlNamespaceRegistry,
            ITypeFactory typeFactory,
            ITypeFeatureProvider featureProvider) : base(xamlNamespaceRegistry, typeFactory, featureProvider)
        {
            _typeFactory = typeFactory;
        }

        public override XamlType GetByType(Type type)
        {
            Guard.ThrowIfNull(type, nameof(type));
            return new AvaloniaXamlType(type, this, _typeFactory, FeatureProvider);
        }
    }
#endif
}