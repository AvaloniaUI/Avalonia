using Avalonia.LogicalTree;

namespace Avalonia.Controls.Documents
{
    internal interface IInlineHost : ILogical
    {
        void AddVisualChild(IControl child);

        void Invalidate();
    }
}
