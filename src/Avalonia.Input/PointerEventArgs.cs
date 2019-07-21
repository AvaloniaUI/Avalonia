// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerEventArgs : RoutedEventArgs
    {
        private readonly IVisual _rootVisual;
        private readonly Point _rootVisualPosition;
        private readonly PointerPointProperties _properties;

        public PointerEventArgs(RoutedEvent routedEvent,
            IInteractive source,
            IPointer pointer,
            IVisual rootVisual, Point rootVisualPosition,
            ulong timestamp,
            PointerPointProperties properties,
            InputModifiers modifiers)
           : base(routedEvent)
        {
            Source = source;
            _rootVisual = rootVisual;
            _rootVisualPosition = rootVisualPosition;
            _properties = properties;
            Pointer = pointer;
            Timestamp = timestamp;
            InputModifiers = modifiers;
        }

        class EmulatedDevice : IPointerDevice
        {
            private readonly PointerEventArgs _ev;

            public EmulatedDevice(PointerEventArgs ev)
            {
                _ev = ev;
            }
            
            public void ProcessRawEvent(RawInputEventArgs ev) => throw new NotSupportedException();

            public IInputElement Captured => _ev.Pointer.Captured;
            public void Capture(IInputElement control)
            {
                _ev.Pointer.Capture(control);
            }

            public Point GetPosition(IVisual relativeTo) => _ev.GetPosition(relativeTo);
        }

        public IPointer Pointer { get; }
        public ulong Timestamp { get; }

        private IPointerDevice _device;

        [Obsolete("Use Pointer to get pointer-specific information")]
        public IPointerDevice Device => _device ?? (_device = new EmulatedDevice(this));

        public InputModifiers InputModifiers { get; }

        public Point GetPosition(IVisual relativeTo)
        {
            if (_rootVisual == null)
                return default;
            if (relativeTo == null)
                return _rootVisualPosition;
            return _rootVisualPosition * _rootVisual.TransformToVisual(relativeTo) ?? default;
        }

        public PointerPoint GetPointerPoint(IVisual relativeTo)
            => new PointerPoint(Pointer, GetPosition(relativeTo), _properties);
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
        private readonly int _obsoleteClickCount;

        public PointerPressedEventArgs(
            IInteractive source,
            IPointer pointer,
            IVisual rootVisual, Point rootVisualPosition,
            ulong timestamp,
            PointerPointProperties properties,
            InputModifiers modifiers,
            int obsoleteClickCount = 1)
            : base(InputElement.PointerPressedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            _obsoleteClickCount = obsoleteClickCount;
        }

        [Obsolete("Use DoubleTapped or DoubleRightTapped event instead")]
        public int ClickCount => _obsoleteClickCount;

        [Obsolete] public MouseButton MouseButton => GetPointerPoint(null).Properties.GetObsoleteMouseButton();
    }

    public class PointerReleasedEventArgs : PointerEventArgs
    {
        public PointerReleasedEventArgs(
            IInteractive source, IPointer pointer,
            IVisual rootVisual, Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, InputModifiers modifiers, MouseButton obsoleteMouseButton)
            : base(InputElement.PointerReleasedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            MouseButton = obsoleteMouseButton;
        }

        [Obsolete()]
        public MouseButton MouseButton { get; private set; }
    }

    public class PointerCaptureLostEventArgs : RoutedEventArgs
    {
        public IPointer Pointer { get; }

        public PointerCaptureLostEventArgs(IInteractive source, IPointer pointer) : base(InputElement.PointerCaptureLostEvent)
        {
            Pointer = pointer;
            Source = source;
        }
    }
}
