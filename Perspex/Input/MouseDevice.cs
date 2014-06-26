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
    using Perspex.Controls;
    using Perspex.Input.Raw;
    using Splat;

    public abstract class MouseDevice : IMouseDevice
    {
        public MouseDevice()
        {
            this.InputManager.RawEventReceived
                .OfType<RawMouseEventArgs>()
                .Where(x => x.Device == this)
                .Subscribe(this.ProcessRawEvent);
        }

        public Interactive Captured
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

        public virtual void Capture(Interactive visual)
        {
            this.Captured = visual;
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
            this.Position = e.Position;

            switch (e.Type)
            {
                case RawMouseEventType.Move:
                    this.MouseMove((IMouseDevice)e.Device, (IVisual)e.Root, e.Position);
                    break;
                case RawMouseEventType.LeftButtonDown:
                    this.MouseDown((IMouseDevice)e.Device, (IVisual)e.Root, e.Position);
                    break;
                case RawMouseEventType.LeftButtonUp:
                    this.MouseUp((IMouseDevice)e.Device, (IVisual)e.Root, e.Position);
                    break;
            }
        }

        private void MouseMove(IMouseDevice device, IVisual visual, Point p)
        {
            if (this.Captured == null)
            {
                this.InputManager.SetPointerOver(this, visual, p);
            }
            else
            {
                Point offset = new Point();

                foreach (IVisual ancestor in this.Captured.GetVisualAncestors())
                {
                    offset += ancestor.Bounds.Position;
                }

                this.InputManager.SetPointerOver(this, this.Captured, p - offset);
            }
        }

        private void MouseDown(IMouseDevice device, IVisual visual, Point p)
        {
            IVisual hit = visual.GetVisualAt(p);

            if (hit != null)
            {
                Interactive interactive = this.Captured ?? (hit as Interactive) ?? hit.GetVisualAncestor<Interactive>();
                IFocusable focusable =
                    this.Captured as IFocusable ??
                    hit.GetVisualAncestorsAndSelf()
                       .OfType<IFocusable>()
                       .FirstOrDefault(x => x.Focusable);

                if (interactive != null)
                {
                    interactive.RaiseEvent(new PointerEventArgs
                    {
                        Device = this,
                        RoutedEvent = Control.PointerPressedEvent,
                        OriginalSource = interactive,
                        Source = interactive,
                    });
                }

                if (focusable != null && focusable.Focusable)
                {
                    focusable.Focus();
                }
            }
        }

        private void MouseUp(IMouseDevice device, IVisual visual, Point p)
        {
            IVisual hit = visual.GetVisualAt(p);

            if (hit != null)
            {
                Interactive source = this.Captured ?? (hit as Interactive) ?? hit.GetVisualAncestor<Interactive>();

                if (source != null)
                {
                    source.RaiseEvent(new PointerEventArgs
                    {
                        Device = this,
                        RoutedEvent = Control.PointerReleasedEvent,
                        OriginalSource = source,
                        Source = source,
                    });
                }
            }
        }
    }
}
