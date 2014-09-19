// -----------------------------------------------------------------------
// <copyright file="Control.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using Perspex.Interactivity;
    using Splat;

    public class InputElement : Interactive, IInputElement
    {
        public static readonly PerspexProperty<bool> FocusableProperty =
            PerspexProperty.Register<InputElement, bool>("Focusable");

        public static readonly PerspexProperty<bool> IsFocusedProperty =
            PerspexProperty.Register<InputElement, bool>("IsFocused", false);

        public static readonly PerspexProperty<bool> IsPointerOverProperty =
            PerspexProperty.Register<InputElement, bool>("IsPointerOver");

        public static readonly RoutedEvent<RoutedEventArgs> GotFocusEvent =
            RoutedEvent.Register<InputElement, RoutedEventArgs>("GotFocus", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<RoutedEventArgs> LostFocusEvent =
            RoutedEvent.Register<InputElement, RoutedEventArgs>("LostFocus", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<KeyEventArgs> KeyDownEvent =
            RoutedEvent.Register<InputElement, KeyEventArgs>("KeyDown", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<KeyEventArgs> PreviewKeyDownEvent =
            RoutedEvent.Register<InputElement, KeyEventArgs>("PreviewKeyDown", RoutingStrategy.Tunnel);

        public static readonly RoutedEvent<PointerEventArgs> PointerEnterEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>("PointerEnter", RoutingStrategy.Direct);

        public static readonly RoutedEvent<PointerEventArgs> PointerLeaveEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>("PointerLeave", RoutingStrategy.Direct);

        public static readonly RoutedEvent<PointerEventArgs> PointerPressedEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>("PointerPressed", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<PointerEventArgs> PointerReleasedEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>("PointerReleased", RoutingStrategy.Bubble);

        public InputElement()
        {
            this.GotFocus += (_, e) => this.OnGotFocus(e);
            this.LostFocus += (_, e) => this.OnLostFocus(e);
            this.KeyDown += (_, e) => this.OnKeyDown(e);
            this.PreviewKeyDown += (_, e) => this.OnPreviewKeyDown(e);
            this.PointerEnter += (_, e) => this.OnPointerEnter(e);
            this.PointerLeave += (_, e) => this.OnPointerLeave(e);
            this.PointerPressed += (_, e) => this.OnPointerPressed(e);
            this.PointerReleased += (_, e) => this.OnPointerReleased(e);
        }

        public event EventHandler<RoutedEventArgs> GotFocus
        {
            add { this.AddHandler(GotFocusEvent, value); }
            remove { this.RemoveHandler(GotFocusEvent, value); }
        }

        public event EventHandler<RoutedEventArgs> LostFocus
        {
            add { this.AddHandler(LostFocusEvent, value); }
            remove { this.RemoveHandler(LostFocusEvent, value); }
        }

        public event EventHandler<KeyEventArgs> KeyDown
        {
            add { this.AddHandler(KeyDownEvent, value); }
            remove { this.RemoveHandler(KeyDownEvent, value); }
        }

        public event EventHandler<KeyEventArgs> PreviewKeyDown
        {
            add { this.AddHandler(PreviewKeyDownEvent, value); }
            remove { this.RemoveHandler(PreviewKeyDownEvent, value); }
        }

        public event EventHandler<PointerEventArgs> PointerEnter
        {
            add { this.AddHandler(PointerEnterEvent, value); }
            remove { this.RemoveHandler(PointerEnterEvent, value); }
        }

        public event EventHandler<PointerEventArgs> PointerLeave
        {
            add { this.AddHandler(PointerLeaveEvent, value); }
            remove { this.RemoveHandler(PointerLeaveEvent, value); }
        }

        public event EventHandler<PointerEventArgs> PointerPressed
        {
            add { this.AddHandler(PointerPressedEvent, value); }
            remove { this.RemoveHandler(PointerPressedEvent, value); }
        }

        public event EventHandler<PointerEventArgs> PointerReleased
        {
            add { this.AddHandler(PointerReleasedEvent, value); }
            remove { this.RemoveHandler(PointerReleasedEvent, value); }
        }

        public bool Focusable
        {
            get { return this.GetValue(FocusableProperty); }
            set { this.SetValue(FocusableProperty, value); }
        }

        public bool IsFocused
        {
            get { return this.GetValue(IsFocusedProperty); }
            private set { this.SetValue(IsFocusedProperty, value); }
        }

        public bool IsPointerOver
        {
            get { return this.GetValue(IsPointerOverProperty); }
            internal set { this.SetValue(IsPointerOverProperty, value); }
        }

        public void Focus()
        {
            Locator.Current.GetService<IFocusManager>().Focus(this);
        }

        protected virtual void OnGotFocus(RoutedEventArgs e)
        {
            this.IsFocused = true;
        }

        protected virtual void OnLostFocus(RoutedEventArgs e)
        {
            this.IsFocused = false;
        }

        protected virtual void OnKeyDown(KeyEventArgs e)
        {
        }

        protected virtual void OnPreviewKeyDown(KeyEventArgs e)
        {
        }

        protected virtual void OnPointerEnter(PointerEventArgs e)
        {
            this.IsPointerOver = true;
        }

        protected virtual void OnPointerLeave(PointerEventArgs e)
        {
            this.IsPointerOver = false;
        }

        protected virtual void OnPointerPressed(PointerEventArgs e)
        {
        }

        protected virtual void OnPointerReleased(PointerEventArgs e)
        {
        }
    }
}
