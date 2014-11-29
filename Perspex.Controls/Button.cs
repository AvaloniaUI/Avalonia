// -----------------------------------------------------------------------
// <copyright file="Button.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Interactivity;

    public class Button : ContentControl
    {
        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<Button, RoutedEventArgs>("Click", RoutingStrategy.Bubble);

        static Button()
        {
            FocusableProperty.OverrideDefaultValue(typeof(Button), true);
        }

        public Button()
        {
            this.PointerPressed += (s, e) =>
            {
                this.Classes.Add(":pressed");
                e.Device.Capture(this);
            };

            this.PointerReleased += (s, e) =>
            {
                e.Device.Capture(null);
                this.Classes.Remove(":pressed");

                if (this.Classes.Contains(":pointerover"))
                {
                    RoutedEventArgs click = new RoutedEventArgs
                    {
                        RoutedEvent = ClickEvent,
                    };
                    this.RaiseEvent(click);
                }
            };
        }

        public event EventHandler<RoutedEventArgs> Click
        {
            add { this.AddHandler(ClickEvent, value); }
            remove { this.RemoveHandler(ClickEvent, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }
    }
}
