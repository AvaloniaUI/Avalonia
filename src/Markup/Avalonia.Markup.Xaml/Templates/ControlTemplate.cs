// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Templates
{
    public class ControlTemplate : IControlTemplate
    {
        [Content]
        [TemplateContent]
        public object Content { get; set; }

        public Type TargetType { get; set; }

        public ControlTemplateResult Build(ITemplatedControl control) => TemplateContent.Load(Content);
    }
}
