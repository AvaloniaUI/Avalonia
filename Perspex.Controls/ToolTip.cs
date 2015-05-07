// -----------------------------------------------------------------------
// <copyright file="ToolTip.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Perspex.Input;
    using Perspex.Threading;
    using Perspex.VisualTree;

    public class ToolTip : ContentControl
    {
        public static readonly PerspexProperty<object> TipProperty =
            PerspexProperty.RegisterAttached<ToolTip, Control, object>("Tip");

        private static PopupRoot popup;

        private static Control current;

        private static Subject<Control> show = new Subject<Control>();

        static ToolTip()
        {
            TipProperty.Changed.Subscribe(TipChanged);
            show.Throttle(TimeSpan.FromSeconds(0.5), PerspexScheduler.Instance).Subscribe(ShowToolTip);
        }

        public static object GetTip(Control element)
        {
            return element.GetValue(TipProperty);
        }

        public static void SetTip(Control element, object value)
        {
            element.SetValue(TipProperty, value);
        }

        private static void TipChanged(PerspexPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;

            if (e.OldValue != null)
            {
                control.PointerEnter -= ControlPointerEnter;
                control.PointerLeave -= ControlPointerLeave;
            }

            if (e.NewValue != null)
            {
                control.PointerEnter += ControlPointerEnter;
                control.PointerLeave += ControlPointerLeave;
            }
        }

        private static void ShowToolTip(Control control)
        {
            if (control != null)
            {
                if (popup == null)
                {
                    popup = new PopupRoot
                    {
                        Content = new ToolTip(),
                    };
                }

                var cp = MouseDevice.Instance.GetPosition(control);
                var position = control.PointToScreen(cp) + new Vector(0, 22);

                popup.Parent = control;
                ((ToolTip)popup.Content).Content = GetTip(control);
                popup.SetPosition(position);
                popup.Show();

                current = control;
            }
        }

        private static void ControlPointerEnter(object sender, PointerEventArgs e)
        {
            var control = (Control)sender;
            show.OnNext(control);
        }

        private static void ControlPointerLeave(object sender, PointerEventArgs e)
        {
            var control = (Control)sender;

            if (control == current)
            {
                popup.Hide();
                show.OnNext(null);
            }
        }
    }
}
