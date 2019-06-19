// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class DynamicResourceExtension : IBinding
    {
        private IResourceNode _anchor;

        public DynamicResourceExtension()
        {
        }

        public DynamicResourceExtension(string resourceKey)
        {
            ResourceKey = resourceKey;
        }

        public object ResourceKey { get; set; }

        public IBinding ProvideValue(IServiceProvider serviceProvider)
        {
            var provideTarget = serviceProvider.GetService<IProvideValueTarget>();

            if (!(provideTarget.TargetObject is IResourceNode))
            {
                _anchor = serviceProvider.GetFirstParent<IResourceNode>();
            }

            return this;
        }

        InstancedBinding IBinding.Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor,
            bool enableDataValidation)
        {
            var control = target as IResourceNode ?? _anchor;

            if (control != null)
            {
                return InstancedBinding.OneWay(control.GetResourceObservable(ResourceKey));
            }

            return null;
        }
    }
}
