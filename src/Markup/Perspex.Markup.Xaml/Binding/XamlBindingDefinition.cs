// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;

namespace Perspex.Markup.Xaml.Binding
{
    public class XamlBindingDefinition
    {
        public XamlBindingDefinition(
            string sourcePropertyPath, 
            BindingMode bindingMode,
            bool isTemplateBinding = false)
        {
            SourcePropertyPath = sourcePropertyPath;
            BindingMode = bindingMode;
            IsTemplateBinding = isTemplateBinding;
        }

        public string SourcePropertyPath { get; }
        public BindingMode BindingMode { get; }
        public bool IsTemplateBinding { get; }
    }
}