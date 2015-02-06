// -----------------------------------------------------------------------
// <copyright file="Button.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Input;
    using Perspex.Interactivity;

    public enum ClickMode
    {
        Release,
        Press,
    }

    public class Button : ContentControl
    {
        public static readonly PerspexProperty<ClickMode> ClickModeProperty =
            PerspexProperty.Register<Button, ClickMode>("ClickMode");

        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<Button, RoutedEventArgs>("Click", RoutingStrategies.Bubble);

        static Button()
        {
            FocusableProperty.OverrideDefaultValue(typeof(Button), true);
            ClickEvent.AddClassHandler<Button>(x => x.OnClick);
        }

        public event EventHandler<RoutedEventArgs> Click
        {
            add { this.AddHandler(ClickEvent, value); }
            remove { this.RemoveHandler(ClickEvent, value); }
        }

        public ClickMode ClickMode
        {
            get { return this.GetValue(ClickModeProperty); }
            set { this.SetValue(ClickModeProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        protected virtual void OnClick(RoutedEventArgs e)
        {
        }

        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            base.OnPointerPressed(e);

            this.Classes.Add(":pressed");
            e.Device.Capture(this);
            e.Handled = true;

            if (this.ClickMode == ClickMode.Press)
            {
                this.RaiseClickEvent();
            }
        }

        protected override void OnPointerReleased(PointerEventArgs e)
        {
            base.OnPointerReleased(e);

            e.Device.Capture(null);
            this.Classes.Remove(":pressed");
            e.Handled = true;

            if (this.ClickMode == ClickMode.Release && this.Classes.Contains(":pointerover"))
            {
                this.RaiseClickEvent();
            }
        }

        private void RaiseClickEvent()
        {
            RoutedEventArgs click = new RoutedEventArgs
            {
                RoutedEvent = ClickEvent,
            };

            this.RaiseEvent(click);
        }
    }
}
