using Avalonia.Collections;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Documents
{
    public interface IInlineHost : ILogical
    {
        void Invalidate();

        IAvaloniaList<Visual> VisualChildren { get; }
    }
}
