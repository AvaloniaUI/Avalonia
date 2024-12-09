using System;

namespace Avalonia.Input;

public class MouseEventArgs : EventArgs
{
    public Point Position { get; }
    public MouseButton Button { get; }

    public MouseEventArgs(Point position, MouseButton button)
    {
        Position = position;
        Button = button;
    }
    
    public static MouseEventArgs Empty => new(new Point(), MouseButton.None);
}
