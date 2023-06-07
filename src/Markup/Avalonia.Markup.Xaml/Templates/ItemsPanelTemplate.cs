using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.Templates
{
    public class ItemsPanelTemplate : ITemplate<Panel?>
    {
        [Content]
        [TemplateContent]
        public object? Content { get; set; }

        public Panel? Build() => (Panel?)TemplateContent.Load(Content)?.Result;

        object? ITemplate.Build() => Build();
    }
}
