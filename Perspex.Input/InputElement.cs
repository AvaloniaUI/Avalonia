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

    public class InputElement : Interactive, IInputElement
    {
        public static readonly PerspexProperty<bool> FocusableProperty =
            PerspexProperty.Register<InputElement, bool>("Focusable");

        public static readonly PerspexProperty<bool> IsEnabledProperty =
            PerspexProperty.Register<InputElement, bool>("IsEnabled", true);

        public static readonly PerspexProperty<bool> IsEnabledCoreProperty =
            PerspexProperty.Register<InputElement, bool>("IsEnabledCore", true);

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

        public static readonly RoutedEvent<PointerEventArgs> PointerMovedEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>("PointerMove", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<PointerEventArgs> PointerPressedEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>("PointerPressed", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<PointerEventArgs> PointerReleasedEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>("PointerReleased", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<PointerEventArgs> PointerWheelChangedEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>("PointerWheelChanged", RoutingStrategy.Bubble);

        static InputElement()
        {
            IsEnabledProperty.Changed.Subscribe(IsEnabledChanged);
        }

        public InputElement()
        {
            this.GotFocus += (_, e) => this.OnGotFocus(e);
            this.LostFocus += (_, e) => this.OnLostFocus(e);
            this.KeyDown += (_, e) => this.OnKeyDown(e);
            this.PreviewKeyDown += (_, e) => this.OnPreviewKeyDown(e);
            this.PointerEnter += (_, e) => this.OnPointerEnter(e);
            this.PointerLeave += (_, e) => this.OnPointerLeave(e);
            this.PointerMoved += (_, e) => this.OnPointerMoved(e);
            this.PointerPressed += (_, e) => this.OnPointerPressed(e);
            this.PointerReleased += (_, e) => this.OnPointerReleased(e);
            this.PointerWheelChanged += (_, e) => this.OnPointerWheelChanged(e);
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

        public event EventHandler<PointerEventArgs> PointerMoved
        {
            add { this.AddHandler(PointerMovedEvent, value); }
            remove { this.RemoveHandler(PointerMovedEvent, value); }
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

        public bool IsPointerOver
        {
            get { return this.GetValue(IsPointerOverProperty); }
            internal set { this.SetValue(IsPointerOverProperty, value); }
        }

        bool IInputElement.IsEnabledCore
        {
            get { return this.IsEnabledCore; }
        }

        protected bool IsEnabledCore
        {
            get { return this.GetValue(IsEnabledCoreProperty); }
            set { this.SetValue(IsEnabledCoreProperty, value); }
        }

        public IInputElement InputHitTest(Point p)
        {
            return this.GetVisualsAt(p)
                .OfType<IInputElement>()
                .Where(x => x.IsEnabledCore)
                .FirstOrDefault();
        }

        public void Focus()
        {
            FocusManager.Instance.Focus(this);
        }

        protected override void OnVisualParentChanged(Visual oldParent)
        {
            this.UpdateIsEnabledCore();
        }

        protected virtual void OnGotFocus(RoutedEventArgs e)
        {
            this.IsFocused = e.OriginalSource == this;
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

        protected virtual void OnPointerMoved(PointerEventArgs e)
        {
        }

        protected virtual void OnPointerPressed(PointerEventArgs e)
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
