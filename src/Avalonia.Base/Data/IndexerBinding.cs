﻿// Copyright (c) The Avalonia Project. All rights reserved.
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

        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor = null,
            bool enableDataValidation = false)
        {
            var mode = Mode == BindingMode.Default ?
                targetProperty.GetMetadata(target.GetType()).DefaultBindingMode :
                Mode;

            switch (mode)
            {
                case BindingMode.OneTime:
                    return InstancedBinding.OneTime(Source.GetObservable(Property));
                case BindingMode.OneWay:
                    return InstancedBinding.OneWay(Source.GetObservable(Property));
                case BindingMode.OneWayToSource:
                    return InstancedBinding.OneWayToSource(Source.GetSubject(Property));
                case BindingMode.TwoWay:
                    return InstancedBinding.TwoWay(Source.GetSubject(Property));
                default:
                    throw new NotSupportedException("Unsupported BindingMode.");
            }
        }
    }
}
