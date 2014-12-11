// -----------------------------------------------------------------------
// <copyright file="MouseDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Input.Raw;
    using Perspex.Interactivity;
    using Perspex.Platform;
    using Splat;

    public abstract class MouseDevice : IMouseDevice
    {
        private int clickCount;

        private Rect lastClickRect;

        private uint lastClickTime;

        public MouseDevice()
        {
            this.InputManager.RawEventReceived
                .OfType<RawMouseEventArgs>()
                .Where(x => x.Device == this)
                .Subscribe(this.ProcessRawEvent);
        }

        public IInteractive Captured
        {
            get;
            protected set;
        }

        public IInputManager InputManager
        {
            get { return Locator.Current.GetService<IInputManager>(); }
        }

        public Point Position
        {
            get;
            protected set;
        }

        public virtual void Capture(IInteractive control)
        {
            this.Captured = control;
        }

        public Point GetPosition(IVisual relativeTo)
        {
            Point p = this.GetClientPosition();
            IVisual v = relativeTo;

            while (v != null)
            {
                p -= v.Bounds.Position;
                v = v.VisualParent;
            }

            return p;
        }

        protected abstract Point GetClientPosition();

        private void ProcessRawEvent(RawMouseEventArgs e)
        {
            var mouse = (IMouseDevice)e.Device;

            this.Position = e.Position;

            switch (e.Type)
            {
                case RawMouseEventType.Move:
                    this.MouseMove(mouse, e.Root, e.Position);
                    break;
                case RawMouseEventType.LeftButtonDown:
                    this.MouseDown(mouse, e.Timestamp, e.Root, e.Position);
                    break;
                case RawMouseEventType.LeftButtonUp:
                    this.MouseUp(mouse, e.Root, e.Position);
                    break;
                case RawMouseEventType.Wheel:
                    this.MouseWheel(mouse, e.Root, e.Position, ((RawMouseWheelEventArgs)e).Delta);
                    break;
            }
        }

        private void MouseMove(IMouseDevice device, IInputElement root, Point p)
        {
            IInteractive source;

            if (this.Captured == null)
            {
                this.InputManager.SetPointerOver(this, root, p);
                source = root as IInteractive;
            }
            else
            {
                Point offset = new Point();

                foreach (IVisual ancestor in this.Captured.GetVisualAncestors())
                {
                    offset += ancestor.Bounds.Position;
                }

                this.InputManager.SetPointerOver(this, this.Captured, p - offset);
                source = this.Captured as IInteractive;
            }

            if (source != null)
            {
                source.RaiseEvent(new PointerEventArgs
                {
                    Device = this,
                    RoutedEvent = InputElement.PointerMovedEvent,
                    OriginalSource = source,
                    Source = source,
                });
            }
        }

        private void MouseDown(IMouseDevice device, uint timestamp, IInputElement root, Point p)
        {
            IVisual hit = root.InputHitTest(p);

            if (hit != null)
            {
                IInteractive source = this.GetSource(hit);

                if (source != null)
                {
                    var settings = Locator.Current.GetService<IPlatformSettings>();
                    var doubleClickTime = settings.DoubleClickTime.TotalMilliseconds;

                    if (!this.lastClickRect.Contains(p) || timestamp - this.lastClickTime > doubleClickTime)
                    {
                        this.clickCount = 0;
                    }

                    ++this.clickCount;
                    this.lastClickTime = timestamp;
                    this.lastClickRect = new Rect(p, new Size())
                        .Inflate(new Thickness(settings.DoubleClickSize.Width / 2, settings.DoubleClickSize.Height / 2));

                    source.RaiseEvent(new PointerPressEventArgs
                    {
                        Device = this,
                        RoutedEvent = InputElement.PointerPressedEvent,
                        OriginalSource = source,
                        Source = source,
                        ClickCount = this.clickCount,
                    });
                }

                IInputElement focusable = this.GetFocusable(hit);

                if (focusable != null && focusable.Focusable)
                {
                    focusable.Focus();
                }
            }
        }

        private void MouseUp(IMouseDevice device, IInputElement root, Point p)
        {
            IVisual hit = root.InputHitTest(p);

            if (hit != null)
            {
                IInteractive source = this.GetSource(hit);

                if (source != null)
                {
                    source.RaiseEvent(new PointerEventArgs
                    {
                        Device = this,
                        RoutedEvent = InputElement.PointerReleasedEvent,
                        OriginalSource = source,
                        Source = source,
                    });
                }
            }
        }

        private void MouseWheel(IMouseDevice device, IInputElement root, Point p,  Vector delta)
        {
            IVisual hit = root.InputHitTest(p);

            if (hit != null)
            {
                IInteractive source = this.GetSource(hit);

                if (source != null)
                {
                    source.RaiseEvent(new PointerWheelEventArgs
                    {
                        Device = this,
                        RoutedEvent = InputElement.PointerWheelChangedEvent,
                        OriginalSource = source,
                        Source = source,
                        Delta = delta,
                    });
                }
            }
        }

        private IInputElement GetFocusable(IVisual hit)
        {
            return this.Captured as IInputElement ?? 
                hit.GetSelfAndVisualAncestors().OfType<IInputElement>().FirstOrDefault(x => x.Focusable);
        }

        private IInteractive GetSource(IVisual hit)
        {
            return this.Captured ?? 
                (hit as IInteractive) ?? 
                hit.GetSelfAndVisualAncestors().OfType<IInteractive>().FirstOrDefault();
        }
    }
}
