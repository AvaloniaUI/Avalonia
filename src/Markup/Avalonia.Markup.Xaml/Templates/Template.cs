// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;
using System.Windows.Markup;

namespace Avalonia.Markup.Xaml.Templates
{
    [ContentProperty(nameof(Content))]
    public class Template : ITemplate<IControl>
    {
        [Content]
        [TemplateContent]
        public object Content { get; set; }

        public IControl Build() => TemplateContent.Load(Content);

        object ITemplate.Build() => Build();
    }
}