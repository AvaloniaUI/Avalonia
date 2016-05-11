// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerEventArgs : RoutedEventArgs
    {
        public PointerEventArgs()
        {

        }

        public PointerEventArgs(RoutedEvent routedEvent)
           : base(routedEvent)
        {

        }

        public IPointerDevice Device { get; set; }

        public InputModifiers InputModifiers { get; set; }

        public Point GetPosition(IVisual relativeTo)
        {
            return Device.GetPosition(relativeTo);
        }
    }
    
    public enum MouseButton
    {
        None,
        Left,
        Right,
        Middle
    }

    public class PointerPressedEventArgs : PointerEventArgs
    {
        public PointerPressedEventArgs()
            : base(InputElement.PointerPressedEvent)
        {
        }

        public PointerPressedEventArgs(RoutedEvent routedEvent)
            : base(routedEvent)
        {
        }

        public int ClickCount { get; set; }
        public MouseButton MouseButton { get; set; }
    }

    public class PointerReleasedEventArgs : PointerEventArgs
    {
        public PointerReleasedEventArgs()
            : base(InputElement.PointerReleasedEvent)
        {
        }

        public PointerReleasedEventArgs(RoutedEvent routedEvent)
            : base(routedEvent)
        {
        }

        public MouseButton MouseButton { get; set; }
    }
}
