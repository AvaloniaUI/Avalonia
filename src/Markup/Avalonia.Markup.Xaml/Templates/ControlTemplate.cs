using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Avalonia.Markup.Xaml.Templates
{
    public class ControlTemplate : IControlTemplate
    {
        [Content]
        [TemplateContent]
        public object? Content { get; set; }

        public Type? TargetType { get; set; }

        public TemplateResult<Control>? Build(TemplatedControl control) => TemplateContent.Load(Content);
    }
}
