// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using OmniXaml;
using Perspex.Controls;
using Perspex.Markup.Xaml.DataBinding;
using Perspex.Markup.Xaml.DataBinding.ChangeTracking;

namespace Perspex.Markup.Xaml.MarkupExtensions
{
    public class BindingExtension : MarkupExtension
    {
        public BindingExtension()
        {
        }

        public BindingExtension(string path)
        {
            Path = path;
        }

        public override object ProvideValue(MarkupExtensionContext extensionContext)
        {
            var target = extensionContext.TargetObject as Control;
            var targetProperty = extensionContext.TargetProperty;
            var targetPropertyName = targetProperty.Name;
            var perspexProperty = target.GetRegisteredProperties().First(property => property.Name == targetPropertyName);

            return new XamlBindingDefinition
                (
                target,
                perspexProperty,
                new PropertyPath(Path),
                Mode == BindingMode.Default ? BindingMode.OneWay : Mode
                );
        }

        /// <summary> The source path (for CLR bindings).</summary>
        public string Path { get; set; }

        public BindingMode Mode { get; set; }
    }
}