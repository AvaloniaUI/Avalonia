// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

#if SYSTEM_XAML
using AmbientAttribute = System.Windows.Markup.AmbientAttribute;
#else
using AmbientAttribute = Portable.Xaml.Markup.AmbientAttribute;
#endif

namespace Avalonia.Markup.Xaml.Templates
{
    [Ambient]
    public class ControlTemplate : IControlTemplate
    {
        [Content]
        public TemplateContent Content { get; set; }

        public Type TargetType { get; set; }

        public IControl Build(ITemplatedControl control) => Content.Load();
    }
}
