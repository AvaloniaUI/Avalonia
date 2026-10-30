using Avalonia;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace ControlCatalog.Controls
{
    public class Code : AvaloniaObject
    {
        public static DirectProperty<Code, string?> TextProperty = AvaloniaProperty.RegisterDirect<Code, string?>(nameof(Code), o => o.Text, (o, v) => o.Text = v);

        public string? Header { get; set; }

        [Content]
        public string? Text
        {
            get => field; 
            set => SetAndRaise(TextProperty, ref field, value);
        }

        public CodeLanguage? Language { get; set; }
    }

    public class Codes : AvaloniaList<Code> { }
}
