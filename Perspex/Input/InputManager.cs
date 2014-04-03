// -----------------------------------------------------------------------
// <copyright file="InputManager.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using Perspex.Controls;
    using Perspex.Input.Raw;

    public class InputManager : IInputManager
    {
        public void Process(RawInputEventArgs e)
        {
            RawMouseEventArgs mouse = e as RawMouseEventArgs;

            if (mouse != null)
            {
                this.ProcessMouse(mouse);
            }
        }

        private void ProcessMouse(RawMouseEventArgs e)
        {
            switch (e.Type)
            {
                case RawMouseEventType.Move:
                    this.MouseMove((IVisual)e.Root, e.Position);
                    break;
                case RawMouseEventType.LeftButtonDown:
                    this.MouseDown((IVisual)e.Root, e.Position);
                    break;
                case RawMouseEventType.LeftButtonUp:
                    this.MouseUp((IVisual)e.Root, e.Position);
                    break;
            }
        }

        private void MouseMove(IVisual visual, Point p)
        {
            Control control = visual as Control;

            if (control != null)
            {
                control.IsPointerOver = visual.Bounds.Contains(p);
            }

            foreach (IVisual child in visual.VisualChildren)
            {
                this.MouseMove(child, p - visual.Bounds.Position);
            }
        }

        private void MouseDown(IVisual visual, Point p)
        {
            IVisual hit = visual.GetVisualAt(p);

            if (hit != null)
            {
                Interactive source = (hit as Interactive) ?? hit.GetVisualAncestor<Interactive>();

                if (source != null)
                {
                    source.RaiseEvent(new PointerEventArgs
                    {
                        RoutedEvent = Control.PointerPressedEvent,
                        OriginalSource = source,
                        Source = source,
                    });
                }
            }
        }

        private void MouseUp(IVisual visual, Point p)
        {
            IVisual hit = visual.GetVisualAt(p);

            if (hit != null)
            {
                Interactive source = (hit as Interactive) ?? hit.GetVisualAncestor<Interactive>();

                if (source != null)
                {
                    source.RaiseEvent(new PointerEventArgs
                    {
                        RoutedEvent = Control.PointerReleasedEvent,
                        OriginalSource = source,
                        Source = source,
                    });
                }
            }
        }
    }
}
