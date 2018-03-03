using System;

namespace Avalonia.Controls.DragDrop
{
    [Flags]
    public enum DragOperation
    {
        None = 0,
        Copy = 1,
        Move = 2,
        Link = 4,
    }
}