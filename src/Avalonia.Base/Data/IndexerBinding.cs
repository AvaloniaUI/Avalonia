// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Data
{
    public class IndexerBinding : IBinding
    {
        public IndexerBinding(
            IAvaloniaObject source,
            AvaloniaProperty property,
            BindingMode mode)
        {
            Source = source;
            Property = property;
            Mode = mode;
        }

        private IAvaloniaObject Source { get; }
        public AvaloniaProperty Property { get; }
        private BindingMode Mode { get; }

        public InstancedBinding Initiate(IAvaloniaObject target, AvaloniaProperty targetProperty, object anchor = null)
        {
            var mode = Mode == BindingMode.Default ?
                targetProperty.GetMetadata(target.GetType()).DefaultBindingMode :
                Mode;

            if (mode == BindingMode.TwoWay)
            {
                return new InstancedBinding(Source.GetSubject(Property), mode);
            }
            else
            {
                return new InstancedBinding(Source.GetObservable(Property), mode);
            }
        }
    }
}
