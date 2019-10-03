// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        public bool IsLeftButtonPressed { get; }
        public bool IsMiddleButtonPressed { get; }
        public bool IsRightButtonPressed { get; }
        public PointerUpdateKind PointerUpdateKind { get; }
        private PointerPointProperties()
        {
            
        }
        
        public PointerPointProperties(RawInputModifiers modifiers, PointerUpdateKind kind)
        {
            PointerUpdateKind = kind;
            IsLeftButtonPressed = modifiers.HasFlagCustom(RawInputModifiers.LeftMouseButton);
            IsMiddleButtonPressed = modifiers.HasFlagCustom(RawInputModifiers.MiddleMouseButton);
            IsRightButtonPressed = modifiers.HasFlagCustom(RawInputModifiers.RightMouseButton);

            // The underlying input source might be reporting the previous state,
            // so make sure that we reflect the current state
            
            if (kind == PointerUpdateKind.LeftButtonPressed)
                IsLeftButtonPressed = true;
            if (kind == PointerUpdateKind.LeftButtonReleased)
                IsLeftButtonPressed = false;
            if (kind == PointerUpdateKind.MiddleButtonPressed)
                IsMiddleButtonPressed = true;
            if (kind == PointerUpdateKind.MiddleButtonReleased)
                IsMiddleButtonPressed = false;
            if (kind == PointerUpdateKind.RightButtonPressed)
                IsRightButtonPressed = true;
            if (kind == PointerUpdateKind.RightButtonReleased)
                IsRightButtonPressed = false;
        }

        public static PointerPointProperties None { get; } = new PointerPointProperties();
        
        public MouseButton GetObsoleteMouseButton()
        {
            if (PointerUpdateKind == PointerUpdateKind.LeftButtonPressed || PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
                return MouseButton.Left;
            if (PointerUpdateKind == PointerUpdateKind.MiddleButtonPressed || PointerUpdateKind == PointerUpdateKind.MiddleButtonReleased)
                return MouseButton.Middle;
            if (PointerUpdateKind == PointerUpdateKind.RightButtonPressed || PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
                return MouseButton.Right;
            return MouseButton.None;
        }
    }

    public enum PointerUpdateKind
    {
        LeftButtonPressed,
        MiddleButtonPressed,
        RightButtonPressed,
        LeftButtonReleased,
        MiddleButtonReleased,
        RightButtonReleased,
        Other
    }
}
