// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Markup.Xaml.Binding
{
    public class XamlBindingDefinition
    {
        public BindingMode Mode { get; set; }
        public BindingPriority Priority { get; set; }
        public RelativeSource RelativeSource { get; set; }
        public string SourcePropertyPath { get; set; }
    }
}