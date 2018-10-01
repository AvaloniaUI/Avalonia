// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Templates
{
    [System.Windows.Markup.Ambient]
    public class ControlTemplate : IControlTemplate
    {
        [Content]
        public TemplateContent Content { get; set; }

        public Type TargetType { get; set; }

        public IControl Build(ITemplatedControl control) => TemplateContent.Load(Content);
    }
}
