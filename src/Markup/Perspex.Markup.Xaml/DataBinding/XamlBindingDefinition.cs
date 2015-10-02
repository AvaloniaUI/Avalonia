// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;

namespace Perspex.Markup.Xaml.DataBinding
{
    public class XamlBindingDefinition
    {
        public XamlBindingDefinition(
            Control target, 
            PerspexProperty targetProperty, 
            string sourcePropertyPath, 
            BindingMode bindingMode)
        {
            Target = target;
            TargetProperty = targetProperty;
            SourcePropertyPath = sourcePropertyPath;
            BindingMode = bindingMode;
        }

        public Control Target { get; }

        public PerspexProperty TargetProperty { get; }

        public string SourcePropertyPath { get; }

        public BindingMode BindingMode { get; }
    }
}