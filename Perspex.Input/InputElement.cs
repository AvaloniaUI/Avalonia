// -----------------------------------------------------------------------
// <copyright file="InputElement.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using System.Linq;
    using Perspex.Interactivity;
    using Perspex.Rendering;
    using Perspex.VisualTree;

    public class InputElement : Interactive, IInputElement
    {
        public static readonly PerspexProperty<bool> FocusableProperty =
            PerspexProperty.Register<InputElement, bool>("Focusable");

        public static readonly PerspexProperty<bool> IsEnabledProperty =
            PerspexProperty.Register<InputElement, bool>("IsEnabled", true);

        public static readonly PerspexProperty<bool> IsEnabledCoreProperty =
            PerspexProperty.Register<InputElement, bool>("IsEnabledCore", true);

        public static readonly PerspexProperty<bool> IsFocusedProperty =
            PerspexProperty.Register<InputElement, bool>("IsFocused");

        public static readonly PerspexProperty<bool> IsHitTestVisibleProperty =
            PerspexProperty.Register<InputElement, bool>("IsHitTestVisible", true);

        public static readonly PerspexProperty<bool> IsPointerOverProperty =
            PerspexProperty.Register<InputElement, bool>("IsPointerOver");

        public static readonly PerspexProperty<bool> IsTabFocusedProperty =
            PerspexProperty.Register<InputElement, bool>("IsTabFocused");

        public static readonly RoutedEvent<GotFocusEventArgs> GotFocusEvent =
            RoutedEvent.Register<InputElement, GotFocusEventArgs>("GotFocus", RoutingStrategies.Bubble);

        public static readonly RoutedEvent<RoutedEventArgs> LostFocusEvent =
            RoutedEvent.Register<InputElement, RoutedEventArgs>("LostFocus", RoutingStrategies.Bubble);

        public static readonly RoutedEvent<KeyEventArgs> KeyDownEvent =
            RoutedEvent.Register<InputElement, KeyEventArgs>(
                "KeyDown", 
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        public static readonly RoutedEvent<KeyEventArgs> KeyUpEvent =
            RoutedEvent.Register<InputElement, KeyEventArgs>(
                "KeyUp",
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        public static readonly RoutedEvent<PointerEventArgs> PointerEnterEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>("PointerEnter", RoutingStrategies.Direct);

        public static readonly RoutedEvent<PointerEventArgs> PointerLeaveEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>("PointerLeave", RoutingStrategies.Direct);

        public static readonly RoutedEvent<PointerEventArgs> PointerMovedEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>(
                "PointerMove", 
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        public static readonly RoutedEvent<PointerPressEventArgs> PointerPressedEvent =
            RoutedEvent.Register<InputElement, PointerPressEventArgs>(
                "PointerPressed", 
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        public static readonly RoutedEvent<PointerEventArgs> PointerReleasedEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>(
                "PointerReleased", 
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        public static readonly RoutedEvent<PointerWheelEventArgs> PointerWheelChangedEvent =
            RoutedEvent.Register<InputElement, PointerWheelEventArgs>(
                "PointerWheelChanged", 
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        static InputElement()
        {
            IsEnabledProperty.Changed.Subscribe(IsEnabledChanged);

            GotFocusEvent.AddClassHandler<InputElement>(x => x.OnGotFocus);
            LostFocusEvent.AddClassHandler<InputElement>(x => x.OnLostFocus);
            KeyDownEvent.AddClassHandler<InputElement>(x => x.OnKeyDown);
            KeyDownEvent.AddClassHandler<InputElement>(x => x.OnKeyUp);
            PointerEnterEvent.AddClassHandler<InputElement>(x => x.OnPointerEnter);
            PointerLeaveEvent.AddClassHandler<InputElement>(x => x.OnPointerLeave);
            PointerMovedEvent.AddClassHandler<InputElement>(x => x.OnPointerMoved);
            PointerPressedEvent.AddClassHandler<InputElement>(x => x.OnPointerPressed);
            PointerReleasedEvent.AddClassHandler<InputElement>(x => x.OnPointerReleased);
            PointerWheelChangedEvent.AddClassHandler<InputElement>(x => x.OnPointerWheelChanged);
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

        public event EventHandler<KeyEventArgs> KeyUp
        {
            add { this.AddHandler(KeyUpEvent, value); }
            remove { this.RemoveHandler(KeyUpEvent, value); }
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

        public event EventHandler<PointerEventArgs> PointerMoved
        {
            add { this.AddHandler(PointerMovedEvent, value); }
            remove { this.RemoveHandler(PointerMovedEvent, value); }
        }

        public event EventHandler<PointerPressEventArgs> PointerPressed
        {
            add { this.AddHandler(PointerPressedEvent, value); }
            remove { this.RemoveHandler(PointerPressedEvent, value); }
        }

        public event EventHandler<PointerEventArgs> PointerReleased
        {
            add { this.AddHandler(PointerReleasedEvent, value); }
            remove { this.RemoveHandler(PointerReleasedEvent, value); }
        }

        public event EventHandler<PointerWheelEventArgs> PointerWheelChanged
        {
            add { this.AddHandler(PointerWheelChangedEvent, value); }
            remove { this.RemoveHandler(PointerWheelChangedEvent, value); }
        }

        public bool Focusable
        {
            get { return this.GetValue(FocusableProperty); }
            set { this.SetValue(FocusableProperty, value); }
        }

        public bool IsEnabled
        {
            get { return this.GetValue(IsEnabledProperty); }
            set { this.SetValue(IsEnabledProperty, value); }
        }

        public bool IsFocused
        {
            get { return this.GetValue(IsFocusedProperty); }
            private set { this.SetValue(IsFocusedProperty, value); }
        }

        public bool IsHitTestVisible
        {
            get { return this.GetValue(IsHitTestVisibleProperty); }
            set { this.SetValue(IsHitTestVisibleProperty, value); }
        }

        public bool IsPointerOver
        {
            get { return this.GetValue(IsPointerOverProperty); }
            internal set { this.SetValue(IsPointerOverProperty, value); }
        }

        bool IInputElement.IsEnabledCore
        {
            get { return this.IsEnabledCore; }
        }

        bool IInputElement.IsTabFocused
        {
            get { return this.GetValue(IsTabFocusedProperty); }
        }

        protected bool IsEnabledCore
        {
            get { return this.GetValue(IsEnabledCoreProperty); }
            set { this.SetValue(IsEnabledCoreProperty, value); }
        }

        public IInputElement InputHitTest(Point p)
        {
            return this.GetInputElementsAt(p).FirstOrDefault();
        }

        public void Focus()
        {
            FocusManager.Instance.Focus(this);
        }

        protected override void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
            base.OnDetachedFromVisualTree(oldRoot);

            if (this.IsFocused)
            {
                FocusManager.Instance.Focus(null);
            }
        }

        protected override void OnVisualParentChanged(Visual oldParent)
        {
            this.UpdateIsEnabledCore();
        }

        protected virtual void OnGotFocus(GotFocusEventArgs e)
        {
            this.IsFocused = e.OriginalSource == this;
            this.SetValue(IsTabFocusedProperty, e.KeyboardNavigated);
        }

        protected virtual void OnLostFocus(RoutedEventArgs e)
        {
            this.IsFocused = false;
            this.SetValue(IsTabFocusedProperty, false);
        }

        protected virtual void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Tab && !e.Handled)
            {
                var shift = (e.Device.Modifiers & ModifierKeys.Shift) != 0;

                if (!shift)
                {
                    KeyboardNavigation.Instance.TabNext(this);
                }
                else
                {
                    KeyboardNavigation.Instance.TabPrevious(this);
                }

                e.Handled = true;
            }
        }

        protected virtual void OnKeyUp(KeyEventArgs e)
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

        protected virtual void OnPointerMoved(PointerEventArgs e)
        {
        }

        protected virtual void OnPointerPressed(PointerPressEventArgs e)
        {
        }

        protected virtual void OnPointerReleased(PointerEventArgs e)
        {
        }

        protected virtual void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
        }

        private static void IsEnabledChanged(PerspexPropertyChangedEventArgs e)
        {
            ((InputElement)e.Sender).UpdateIsEnabledCore();
        }

        private void UpdateIsEnabledCore()
        {
            this.UpdateIsEnabledCore(this.GetVisualParent<InputElement>());
        }

        private void UpdateIsEnabledCore(InputElement parent)
        {
            if (parent != null)
            {
                this.IsEnabledCore = this.IsEnabled && parent.IsEnabledCore;
            }
            else
            {
                this.IsEnabledCore = this.IsEnabled;
            }

            foreach (var child in this.GetVisualChildren().OfType<InputElement>())
            {
                child.UpdateIsEnabledCore(this);
            }
        }
    }
}
