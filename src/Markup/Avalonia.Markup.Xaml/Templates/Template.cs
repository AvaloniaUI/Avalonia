using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Templates
{
    public class Template : ITemplate<Control?>
    {
        [Content]
        [TemplateContent]
        public object? Content { get; set; }

        public Control? Build() => TemplateContent.Load(Content)?.Result;

        object? ITemplate.Build() => Build();
    }
}
