using System;

namespace Avalonia.Controls
{
    public interface IVirtualizingPanel : IPanel
    {
        bool IsFull { get; }

        int OverflowCount { get; }

        double AverageItemSize { get; }

        double PixelOffset { get; set; }
    }
}
