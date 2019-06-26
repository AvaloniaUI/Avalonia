// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Implements input-related functionality for a control.
    /// </summary>
    public class InputElement : Interactive, IInputElement
    {
        /// <summary>
        /// Defines the <see cref="Focusable"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> FocusableProperty =
            AvaloniaProperty.Register<InputElement, bool>(nameof(Focusable));

        /// <summary>
        /// Defines the <see cref="IsEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsEnabledProperty =
            AvaloniaProperty.Register<InputElement, bool>(nameof(IsEnabled), true);

        /// <summary>
        /// Defines the <see cref="IsEffectivelyEnabled"/> property.
        /// </summary>
        public static readonly DirectProperty<InputElement, bool> IsEffectivelyEnabledProperty =
            AvaloniaProperty.RegisterDirect<InputElement, bool>(
                nameof(IsEffectivelyEnabled),
                o => o.IsEffectivelyEnabled);

        /// <summary>
        /// Gets or sets associated mouse cursor.
        /// </summary>
        public static readonly StyledProperty<Cursor> CursorProperty =
            AvaloniaProperty.Register<InputElement, Cursor>(nameof(Cursor), null, true);

        /// <summary>
        /// Defines the <see cref="IsFocused"/> property.
        /// </summary>
        public static readonly DirectProperty<InputElement, bool> IsFocusedProperty =
            AvaloniaProperty.RegisterDirect<InputElement, bool>(nameof(IsFocused), o => o.IsFocused);

        /// <summary>
        /// Defines the <see cref="IsHitTestVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsHitTestVisibleProperty =
            AvaloniaProperty.Register<InputElement, bool>(nameof(IsHitTestVisible), true);

        /// <summary>
        /// Defines the <see cref="IsPointerOver"/> property.
        /// </summary>
        public static readonly DirectProperty<InputElement, bool> IsPointerOverProperty =
            AvaloniaProperty.RegisterDirect<InputElement, bool>(nameof(IsPointerOver), o => o.IsPointerOver);

        /// <summary>
        /// Defines the <see cref="GotFocus"/> event.
        /// </summary>
        public static readonly RoutedEvent<GotFocusEventArgs> GotFocusEvent =
            RoutedEvent.Register<InputElement, GotFocusEventArgs>(nameof(GotFocus), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="LostFocus"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> LostFocusEvent =
            RoutedEvent.Register<InputElement, RoutedEventArgs>(nameof(LostFocus), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="KeyDown"/> event.
        /// </summary>
        public static readonly RoutedEvent<KeyEventArgs> KeyDownEvent =
            RoutedEvent.Register<InputElement, KeyEventArgs>(
                "KeyDown",
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="KeyUp"/> event.
        /// </summary>
        public static readonly RoutedEvent<KeyEventArgs> KeyUpEvent =
            RoutedEvent.Register<InputElement, KeyEventArgs>(
                "KeyUp",
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="TextInput"/> event.
        /// </summary>
        public static readonly RoutedEvent<TextInputEventArgs> TextInputEvent =
            RoutedEvent.Register<InputElement, TextInputEventArgs>(
                "TextInput",
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerEnter"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerEventArgs> PointerEnterEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>(nameof(PointerEnter), RoutingStrategies.Direct);

        /// <summary>
        /// Defines the <see cref="PointerLeave"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerEventArgs> PointerLeaveEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>(nameof(PointerLeave), RoutingStrategies.Direct);

        /// <summary>
        /// Defines the <see cref="PointerMoved"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerEventArgs> PointerMovedEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>(
                "PointerMove",
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerPressed"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerPressedEventArgs> PointerPressedEvent =
            RoutedEvent.Register<InputElement, PointerPressedEventArgs>(
                "PointerPressed",
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerReleased"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerReleasedEventArgs> PointerReleasedEvent =
            RoutedEvent.Register<InputElement, PointerReleasedEventArgs>(
                "PointerReleased",
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        
        /// <summary>
        /// Defines the <see cref="PointerCaptureLost"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<PointerCaptureLostEventArgs> PointerCaptureLostEvent =
            RoutedEvent.Register<InputElement, PointerCaptureLostEventArgs>(
                "PointerCaptureLost", 
                RoutingStrategies.Direct);

        /// <summary>
        /// Defines the <see cref="PointerWheelChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerWheelEventArgs> PointerWheelChangedEvent =
            RoutedEvent.Register<InputElement, PointerWheelEventArgs>(
                "PointerWheelChanged",
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Tapped"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> TappedEvent = Gestures.TappedEvent;

        /// <summary>
        /// Defines the <see cref="DoubleTapped"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> DoubleTappedEvent = Gestures.DoubleTappedEvent;

        private bool _isEffectivelyEnabled = true;
        private bool _isFocused;
        private bool _isPointerOver;
        private GestureRecognizerCollection _gestureRecognizers;

        /// <summary>
        /// Initializes static members of the <see cref="InputElement"/> class.
        /// </summary>
        static InputElement()
        {
            IsEnabledProperty.Changed.Subscribe(IsEnabledChanged);

            GotFocusEvent.AddClassHandler<InputElement>(x => x.OnGotFocus);
            LostFocusEvent.AddClassHandler<InputElement>(x => x.OnLostFocus);
            KeyDownEvent.AddClassHandler<InputElement>(x => x.OnKeyDown);
            KeyUpEvent.AddClassHandler<InputElement>(x => x.OnKeyUp);
            TextInputEvent.AddClassHandler<InputElement>(x => x.OnTextInput);
            PointerEnterEvent.AddClassHandler<InputElement>(x => x.OnPointerEnterCore);
            PointerLeaveEvent.AddClassHandler<InputElement>(x => x.OnPointerLeaveCore);
            PointerMovedEvent.AddClassHandler<InputElement>(x => x.OnPointerMoved);
            PointerPressedEvent.AddClassHandler<InputElement>(x => x.OnPointerPressed);
            PointerReleasedEvent.AddClassHandler<InputElement>(x => x.OnPointerReleased);
            PointerCaptureLostEvent.AddClassHandler<InputElement>(x => x.OnPointerCaptureLost);
            PointerWheelChangedEvent.AddClassHandler<InputElement>(x => x.OnPointerWheelChanged);

            PseudoClass<InputElement, bool>(IsEffectivelyEnabledProperty, x => !x, ":disabled");
            PseudoClass<InputElement>(IsFocusedProperty, ":focus");
            PseudoClass<InputElement>(IsPointerOverProperty, ":pointerover");
        }

        /// <summary>
        /// Occurs when the control receives focus.
        /// </summary>
        public event EventHandler<GotFocusEventArgs> GotFocus
        {
            add { AddHandler(GotFocusEvent, value); }
            remove { RemoveHandler(GotFocusEvent, value); }
        }

        /// <summary>
        /// Occurs when the control loses focus.
        /// </summary>
        public event EventHandler<RoutedEventArgs> LostFocus
        {
            add { AddHandler(LostFocusEvent, value); }
            remove { RemoveHandler(LostFocusEvent, value); }
        }

        /// <summary>
        /// Occurs when a key is pressed while the control has focus.
        /// </summary>
        public event EventHandler<KeyEventArgs> KeyDown
        {
            add { AddHandler(KeyDownEvent, value); }
            remove { RemoveHandler(KeyDownEvent, value); }
        }

        /// <summary>
        /// Occurs when a key is released while the control has focus.
        /// </summary>
        public event EventHandler<KeyEventArgs> KeyUp
        {
            add { AddHandler(KeyUpEvent, value); }
            remove { RemoveHandler(KeyUpEvent, value); }
        }

        /// <summary>
        /// Occurs when a user typed some text while the control has focus.
        /// </summary>
        public event EventHandler<TextInputEventArgs> TextInput
        {
            add { AddHandler(TextInputEvent, value); }
            remove { RemoveHandler(TextInputEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer enters the control.
        /// </summary>
        public event EventHandler<PointerEventArgs> PointerEnter
        {
            add { AddHandler(PointerEnterEvent, value); }
            remove { RemoveHandler(PointerEnterEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer leaves the control.
        /// </summary>
        public event EventHandler<PointerEventArgs> PointerLeave
        {
            add { AddHandler(PointerLeaveEvent, value); }
            remove { RemoveHandler(PointerLeaveEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer moves over the control.
        /// </summary>
        public event EventHandler<PointerEventArgs> PointerMoved
        {
            add { AddHandler(PointerMovedEvent, value); }
            remove { RemoveHandler(PointerMovedEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer is pressed over the control.
        /// </summary>
        public event EventHandler<PointerPressedEventArgs> PointerPressed
        {
            add { AddHandler(PointerPressedEvent, value); }
            remove { RemoveHandler(PointerPressedEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer is released over the control.
        /// </summary>
        public event EventHandler<PointerReleasedEventArgs> PointerReleased
        {
            add { AddHandler(PointerReleasedEvent, value); }
            remove { RemoveHandler(PointerReleasedEvent, value); }
        }

        /// <summary>
        /// Occurs when the control or its child control loses the pointer capture for any reason,
        /// event will not be triggered for a parent control if capture was transferred to another child of that parent control
        /// </summary>
        public event EventHandler<PointerCaptureLostEventArgs> PointerCaptureLost
        {
            add => AddHandler(PointerCaptureLostEvent, value);
            remove => RemoveHandler(PointerCaptureLostEvent, value);
        }
        
        /// <summary>
        /// Occurs when the mouse wheen is scrolled over the control.
        /// </summary>
        public event EventHandler<PointerWheelEventArgs> PointerWheelChanged
        {
            add { AddHandler(PointerWheelChangedEvent, value); }
            remove { RemoveHandler(PointerWheelChangedEvent, value); }
        }

        /// <summary>
        /// Occurs when a tap gesture occurs on the control.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Tapped
        {
            add { AddHandler(TappedEvent, value); }
            remove { RemoveHandler(TappedEvent, value); }
        }

        /// <summary>
        /// Occurs when a double-tap gesture occurs on the control.
        /// </summary>
        public event EventHandler<RoutedEventArgs> DoubleTapped
        {
            add { AddHandler(DoubleTappedEvent, value); }
            remove { RemoveHandler(DoubleTappedEvent, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control can receive focus.
        /// </summary>
        public bool Focusable
        {
            get { return GetValue(FocusableProperty); }
            set { SetValue(FocusableProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control is enabled for user interaction.
        /// </summary>
        public bool IsEnabled
        {
            get { return GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets associated mouse cursor.
        /// </summary>
        public Cursor Cursor
        {
            get { return GetValue(CursorProperty); }
            set { SetValue(CursorProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control is focused.
        /// </summary>
        public bool IsFocused
        {
            get { return _isFocused; }
            private set { SetAndRaise(IsFocusedProperty, ref _isFocused, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control is considered for hit testing.
        /// </summary>
        public bool IsHitTestVisible
        {
            get { return GetValue(IsHitTestVisibleProperty); }
            set { SetValue(IsHitTestVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the pointer is currently over the control.
        /// </summary>
        public bool IsPointerOver
        {
            get { return _isPointerOver; }
            internal set { SetAndRaise(IsPointerOverProperty, ref _isPointerOver, value); }
        }

        /// <inheritdoc/>
        public bool IsEffectivelyEnabled
        {
            get => _isEffectivelyEnabled;
            private set => SetAndRaise(IsEffectivelyEnabledProperty, ref _isEffectivelyEnabled, value);
        }

        public List<KeyBinding> KeyBindings { get; } = new List<KeyBinding>();

        /// <summary>
        /// Allows a derived class to override the enabled state of the control.
        /// </summary>
        /// <remarks>
        /// Derived controls may wish to disable the enabled state of the control without overwriting the
        /// user-supplied <see cref="IsEnabled"/> setting. This can be done by overriding this property
        /// to return the overridden enabled state. If the value returned from <see cref="IsEnabledCore"/>
        /// should change, then the derived control should call <see cref="UpdateIsEffectivelyEnabled()"/>.
        /// </remarks>
        protected virtual bool IsEnabledCore => IsEnabled;

        public GestureRecognizerCollection GestureRecognizers
            => _gestureRecognizers ?? (_gestureRecognizers = new GestureRecognizerCollection(this));

        /// <summary>
        /// Focuses the control.
        /// </summary>
        public void Focus()
        {
            FocusManager.Instance?.Focus(this);
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTreeCore(e);

            if (IsFocused)
            {
                FocusManager.Instance.Focus(null);
            }
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTreeCore(e);
            UpdateIsEffectivelyEnabled();
        }

        /// <summary>
        /// Called before the <see cref="GotFocus"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnGotFocus(GotFocusEventArgs e)
        {
            IsFocused = e.Source == this;
        }

        /// <summary>
        /// Called before the <see cref="LostFocus"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnLostFocus(RoutedEventArgs e)
        {
            IsFocused = false;
        }

        /// <summary>
        /// Called before the <see cref="KeyDown"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyDown(KeyEventArgs e)
        {
        }

        /// <summary>
        /// Called before the <see cref="KeyUp"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyUp(KeyEventArgs e)
        {
        }

        /// <summary>
        /// Called before the <see cref="TextInput"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnTextInput(TextInputEventArgs e)
        {
        }

        /// <summary>
        /// Called before the <see cref="PointerEnter"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerEnter(PointerEventArgs e)
        {
        }

        /// <summary>
        /// Called before the <see cref="PointerLeave"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerLeave(PointerEventArgs e)
        {
        }

        /// <summary>
        /// Called before the <see cref="PointerMoved"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerMoved(PointerEventArgs e)
        {
            if (_gestureRecognizers?.HandlePointerMoved(e) == true)
                e.Handled = true;
        }

        /// <summary>
        /// Called before the <see cref="PointerPressed"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_gestureRecognizers?.HandlePointerPressed(e) == true)
                e.Handled = true;
        }

        /// <summary>
        /// Called before the <see cref="PointerReleased"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_gestureRecognizers?.HandlePointerReleased(e) == true)
                e.Handled = true;
        }

        /// <summary>
        /// Called before the <see cref="PointerCaptureLost"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            _gestureRecognizers?.HandlePointerCaptureLost(e);
        }

        /// <summary>
        /// Called before the <see cref="PointerWheelChanged"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
        }

        /// <summary>
        /// Updates the <see cref="IsEffectivelyEnabled"/> property value according to the parent
        /// control's enabled state and <see cref="IsEnabledCore"/>.
        /// </summary>
        protected void UpdateIsEffectivelyEnabled()
        {
            UpdateIsEffectivelyEnabled(this.GetVisualParent<InputElement>());
        }

        private static void IsEnabledChanged(AvaloniaPropertyChangedEventArgs e)
        {
            ((InputElement)e.Sender).UpdateIsEffectivelyEnabled();
        }

        /// <summary>
        /// Called before the <see cref="PointerEnter"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void OnPointerEnterCore(PointerEventArgs e)
        {
            IsPointerOver = true;
            OnPointerEnter(e);
        }

        /// <summary>
        /// Called before the <see cref="PointerLeave"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void OnPointerLeaveCore(PointerEventArgs e)
        {
            IsPointerOver = false;
            OnPointerLeave(e);
        }

        /// <summary>
        /// Updates the <see cref="IsEffectivelyEnabled"/> property based on the parent's
        /// <see cref="IsEffectivelyEnabled"/>.
        /// </summary>
        /// <param name="parent">The parent control.</param>
        private void UpdateIsEffectivelyEnabled(InputElement parent)
        {
            IsEffectivelyEnabled = IsEnabledCore && (parent?.IsEffectivelyEnabled ?? true);

            foreach (var child in this.GetVisualChildren().OfType<InputElement>())
            {
                child.UpdateIsEffectivelyEnabled(this);
            }
        }
    }
}
