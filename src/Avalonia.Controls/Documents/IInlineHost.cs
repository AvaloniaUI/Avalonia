using Avalonia.Collections;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Documents
{
    internal interface IInlineHost : ILogical, ITextScaleable
    {
        void Invalidate();

        IAvaloniaList<Visual> VisualChildren { get; }
    }
}
