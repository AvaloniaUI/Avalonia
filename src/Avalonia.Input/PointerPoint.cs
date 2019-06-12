namespace Avalonia.Input
{
    public sealed class PointerPoint
    {
        public PointerPoint(IPointer pointer, Point position, PointerPointProperties properties)
        {
            Pointer = pointer;
            Position = position;
            Properties = properties;
        }
        public IPointer Pointer { get; }
        public PointerPointProperties Properties { get; }
        public Point Position { get; }
    }

    public sealed class PointerPointProperties
    {
        public bool IsLeftButtonPressed { get; set; }
        public bool IsMiddleButtonPressed { get; set; }
        public bool IsRightButtonPressed { get; set; }

        public PointerPointProperties()
        {
            
        }
        
        public PointerPointProperties(InputModifiers modifiers)
        {
            IsLeftButtonPressed = modifiers.HasFlag(InputModifiers.LeftMouseButton);
            IsMiddleButtonPressed = modifiers.HasFlag(InputModifiers.MiddleMouseButton);
            IsRightButtonPressed = modifiers.HasFlag(InputModifiers.RightMouseButton);
        }
        
        public MouseButton GetObsoleteMouseButton()
        {
            if (IsLeftButtonPressed)
                return MouseButton.Left;
            if (IsMiddleButtonPressed)
                return MouseButton.Middle;
            if (IsRightButtonPressed)
                return MouseButton.Right;
            return MouseButton.None;
        }
    }
}
