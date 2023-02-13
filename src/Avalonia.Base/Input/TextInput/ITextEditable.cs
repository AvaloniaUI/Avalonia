using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    [NotClientImplementable]
    public interface ITextEditable
    {
        event EventHandler TextChanged;
        event EventHandler SelectionChanged;
        event EventHandler CompositionChanged;
        int SelectionStart { get; set; }
        int SelectionEnd { get; set; }
        int CompositionStart { get; }
        int CompositionEnd { get; }
        
        string? Text { get; set; }
    }
}
