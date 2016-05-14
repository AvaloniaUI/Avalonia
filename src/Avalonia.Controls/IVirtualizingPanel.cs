using System;

namespace Avalonia.Controls
{
    public interface IVirtualizingPanel : IPanel
    {
        bool IsFull { get; }

        int OverflowCount { get; }

        Action ArrangeCompleted { get; set; }
    }
}
