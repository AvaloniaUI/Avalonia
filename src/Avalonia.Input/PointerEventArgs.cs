using System;
using System.Collections.Generic;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class PointerEventArgs : RoutedEventArgs
    {
        private readonly IVisual? _rootVisual;
        private readonly Point _rootVisualPosition;
        private readonly PointerPointProperties _properties;
        private Lazy<IReadOnlyList<RawPointerPoint>?>? _previousPoints;

        public PointerEventArgs(RoutedEvent routedEvent,
            IInteractive? source,
            IPointer pointer,
            IVisual? rootVisual, Point rootVisualPosition,
            ulong timestamp,
            PointerPointProperties properties,
            KeyModifiers modifiers)
           : base(routedEvent)
        {
            Source = source;
            _rootVisual = rootVisual;
            _rootVisualPosition = rootVisualPosition;
            _properties = properties;
            Pointer = pointer;
            Timestamp = timestamp;
            KeyModifiers = modifiers;
        }
        
        public PointerEventArgs(RoutedEvent routedEvent,
            IInteractive? source,
            IPointer pointer,
            IVisual? rootVisual, Point rootVisualPosition,
            ulong timestamp,
            PointerPointProperties properties,
            KeyModifiers modifiers,
            Lazy<IReadOnlyList<RawPointerPoint>?>? previousPoints)
            : this(routedEvent, source, pointer, rootVisual, rootVisualPosition, timestamp, properties, modifiers)
        {
            _previousPoints = previousPoints;
        }
        

        class EmulatedDevice : IPointerDevice
        {
            private readonly PointerEventArgs _ev;

            public EmulatedDevice(PointerEventArgs ev)
            {
                _ev = ev;
            }
            
            public void ProcessRawEvent(RawInputEventArgs ev) => throw new NotSupportedException();

            public IInputElement? Captured => _ev.Pointer.Captured;
            public void Capture(IInputElement? control)
            {
                _ev.Pointer.Capture(control);
            }

            public Point GetPosition(IVisual relativeTo) => _ev.GetPosition(relativeTo);
        }

        public IPointer Pointer { get; }
        public ulong Timestamp { get; }

        private IPointerDevice? _device;

        [Obsolete("Use Pointer to get pointer-specific information")]
        public IPointerDevice Device => _device ?? (_device = new EmulatedDevice(this));

        [Obsolete("Use KeyModifiers and PointerPointProperties")]
        public InputModifiers InputModifiers 
        {
            get
            {
                var mods = (InputModifiers)KeyModifiers;
                if (_properties.IsLeftButtonPressed)
                    mods |= InputModifiers.LeftMouseButton;
                if (_properties.IsMiddleButtonPressed)
                    mods |= InputModifiers.MiddleMouseButton;
                if (_properties.IsRightButtonPressed)
                    mods |= InputModifiers.RightMouseButton;
                
                return mods;
            }
        }
        
        public KeyModifiers KeyModifiers { get; }

        private Point GetPosition(Point pt, IVisual? relativeTo)
        {
            if (_rootVisual == null)
                return default;
            if (relativeTo == null)
                return pt;
            return pt * _rootVisual.TransformToVisual(relativeTo) ?? default;
        }
        
        public Point GetPosition(IVisual? relativeTo) => GetPosition(_rootVisualPosition, relativeTo);

        [Obsolete("Use GetCurrentPoint")]
        public PointerPoint GetPointerPoint(IVisual? relativeTo) => GetCurrentPoint(relativeTo);
        
        /// <summary>
        /// Returns the PointerPoint associated with the current event
        /// </summary>
        /// <param name="relativeTo">The visual which coordinate system to use. Pass null for toplevel coordinate system</param>
        /// <returns></returns>
        public PointerPoint GetCurrentPoint(IVisual? relativeTo)
            => new PointerPoint(Pointer, GetPosition(relativeTo), _properties);

        /// <summary>
        /// Returns the PointerPoint associated with the current event
        /// </summary>
        /// <param name="relativeTo">The visual which coordinate system to use. Pass null for toplevel coordinate system</param>
        /// <returns></returns>
        public IReadOnlyList<PointerPoint> GetIntermediatePoints(IVisual? relativeTo)
        {
            var previousPoints = _previousPoints?.Value;            
            if (previousPoints == null || previousPoints.Count == 0)
                return new[] { GetCurrentPoint(relativeTo) };
            var points = new PointerPoint[previousPoints.Count + 1];
            for (var c = 0; c < previousPoints.Count; c++)
            {
                var pt = previousPoints[c];
                points[c] = new PointerPoint(Pointer, GetPosition(pt.Position, relativeTo), _properties);
            }

            points[points.Length - 1] = GetCurrentPoint(relativeTo);
            return points;
        }

        /// <summary>
        /// Returns the current pointer point properties
        /// </summary>
        protected PointerPointProperties Properties => _properties;
    }
    
    public enum MouseButton
    {
        None,
        Left,
        Right,
        Middle,
        XButton1,
        XButton2
    }

    public class PointerPressedEventArgs : PointerEventArgs
    {
        private readonly int _clickCount;

        public PointerPressedEventArgs(
            IInteractive source,
            IPointer pointer,
            IVisual rootVisual, Point rootVisualPosition,
            ulong timestamp,
            PointerPointProperties properties,
            KeyModifiers modifiers,
            int clickCount = 1)
            : base(InputElement.PointerPressedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            _clickCount = clickCount;
        }

        public int ClickCount => _clickCount;

        [Obsolete("Use PointerPressedEventArgs.GetCurrentPoint(this).Properties")]
        public MouseButton MouseButton => Properties.PointerUpdateKind.GetMouseButton();
    }

    public class PointerReleasedEventArgs : PointerEventArgs
    {
        public PointerReleasedEventArgs(
            IInteractive source, IPointer pointer,
            IVisual rootVisual, Point rootVisualPosition, ulong timestamp,
            PointerPointProperties properties, KeyModifiers modifiers,
            MouseButton initialPressMouseButton)
            : base(InputElement.PointerReleasedEvent, source, pointer, rootVisual, rootVisualPosition,
                timestamp, properties, modifiers)
        {
            InitialPressMouseButton = initialPressMouseButton;
        }

        /// <summary>
        /// Gets the mouse button that triggered the corresponding PointerPressed event
        /// </summary>
        public MouseButton InitialPressMouseButton { get; }

        [Obsolete("Use InitialPressMouseButton")]
        public MouseButton MouseButton => InitialPressMouseButton;
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
