using System;

namespace Avalonia.Input.DragDrop
{
    [Flags]
    public enum DragDropEffects
    {
        None = 0,
        Copy = 1,
        Move = 2,
        Link = 4,
    }
}