#nullable enable

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Implements input-related functionality for a control.
    /// </summary>
    [PseudoClasses(":disabled", ":focus", ":focus-visible", ":focus-within", ":pointerover")]
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
        public static readonly StyledProperty<Cursor?> CursorProperty =
            AvaloniaProperty.Register<InputElement, Cursor?>(nameof(Cursor), null, true);

        /// <summary>
        /// Defines the <see cref="IsKeyboardFocusWithin"/> property.
        /// </summary>
        public static readonly DirectProperty<InputElement, bool> IsKeyboardFocusWithinProperty =
            AvaloniaProperty.RegisterDirect<InputElement, bool>(
                nameof(IsKeyboardFocusWithin),
                o => o.IsKeyboardFocusWithin);

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
        /// Defines the <see cref="IsTabStop"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsTabStopProperty =
            KeyboardNavigation.IsTabStopProperty.AddOwner<InputElement>();

        /// <summary>
        /// Defines the <see cref="GotFocus"/> event.
        /// </summary>
        public static readonly RoutedEvent<GotFocusEventArgs> GotFocusEvent =
            RoutedEvent.Register<InputElement, GotFocusEventArgs>(nameof(GotFocus), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="GettingFocus"/> event.
        /// </summary>
        public static readonly RoutedEvent<FocusChangingEventArgs> GettingFocusEvent =
            RoutedEvent.Register<InputElement, FocusChangingEventArgs>(nameof(GettingFocus), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="LostFocus"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> LostFocusEvent =
            RoutedEvent.Register<InputElement, RoutedEventArgs>(nameof(LostFocus), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="LosingFocus"/> event.
        /// </summary>
        public static readonly RoutedEvent<FocusChangingEventArgs> LosingFocusEvent =
            RoutedEvent.Register<InputElement, FocusChangingEventArgs>(nameof(LosingFocus), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="KeyDown"/> event.
        /// </summary>
        public static readonly RoutedEvent<KeyEventArgs> KeyDownEvent =
            RoutedEvent.Register<InputElement, KeyEventArgs>(
                nameof(KeyDown),
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="KeyUp"/> event.
        /// </summary>
        public static readonly RoutedEvent<KeyEventArgs> KeyUpEvent =
            RoutedEvent.Register<InputElement, KeyEventArgs>(
                nameof(KeyUp),
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="TabIndex"/> property.
        /// </summary>
        public static readonly StyledProperty<int> TabIndexProperty =
            KeyboardNavigation.TabIndexProperty.AddOwner<InputElement>();

        /// <summary>
        /// Defines the <see cref="TextInput"/> event.
        /// </summary>
        public static readonly RoutedEvent<TextInputEventArgs> TextInputEvent =
            RoutedEvent.Register<InputElement, TextInputEventArgs>(
                nameof(TextInput),
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="TextInputMethodClientRequested"/> event.
        /// </summary>
        public static readonly RoutedEvent<TextInputMethodClientRequestedEventArgs> TextInputMethodClientRequestedEvent =
            RoutedEvent.Register<InputElement, TextInputMethodClientRequestedEventArgs>(
                nameof(TextInputMethodClientRequested),
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerEntered"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerEventArgs> PointerEnteredEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>(
                nameof(PointerEntered),
                RoutingStrategies.Direct);

        /// <summary>
        /// Defines the <see cref="PointerExited"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerEventArgs> PointerExitedEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>(
                nameof(PointerExited),
                RoutingStrategies.Direct);

        /// <summary>
        /// Defines the <see cref="PointerMoved"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerEventArgs> PointerMovedEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>(
                nameof(PointerMoved),
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerPressed"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerPressedEventArgs> PointerPressedEvent =
            RoutedEvent.Register<InputElement, PointerPressedEventArgs>(
                nameof(PointerPressed),
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerReleased"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerReleasedEventArgs> PointerReleasedEvent =
            RoutedEvent.Register<InputElement, PointerReleasedEventArgs>(
                nameof(PointerReleased),
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerCaptureChanging"/> routed event.
        /// </summary>
        internal static readonly RoutedEvent<PointerCaptureChangingEventArgs> PointerCaptureChangingEvent =
            RoutedEvent.Register<InputElement, PointerCaptureChangingEventArgs>(
                nameof(PointerCaptureChanging),
                RoutingStrategies.Direct);

        /// <summary>
        /// Defines the <see cref="PointerCaptureLost"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<PointerCaptureLostEventArgs> PointerCaptureLostEvent =
            RoutedEvent.Register<InputElement, PointerCaptureLostEventArgs>(
                nameof(PointerCaptureLost),
                RoutingStrategies.Direct);

        /// <summary>
        /// Defines the <see cref="PointerWheelChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerWheelEventArgs> PointerWheelChangedEvent =
            RoutedEvent.Register<InputElement, PointerWheelEventArgs>(
                nameof(PointerWheelChanged),
                RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Tapped"/> event.
        /// </summary>
        public static readonly RoutedEvent<TappedEventArgs> TappedEvent = Gestures.TappedEvent;

        /// <summary>
        /// Defines the <see cref="RightTapped"/> event.
        /// </summary>
        public static readonly RoutedEvent<TappedEventArgs> RightTappedEvent = Gestures.RightTappedEvent;

        /// <summary>
        /// Defines the <see cref="Holding"/> event.
        /// </summary>
        public static readonly RoutedEvent<HoldingRoutedEventArgs> HoldingEvent = Gestures.HoldingEvent;

        /// <summary>
        /// Defines the <see cref="DoubleTapped"/> event.
        /// </summary>
        public static readonly RoutedEvent<TappedEventArgs> DoubleTappedEvent = Gestures.DoubleTappedEvent;

        private bool _isEffectivelyEnabled = true;
        private bool _isFocused;
        private bool _isKeyboardFocusWithin;
        private bool _isFocusVisible;
        private bool _isPointerOver;
        private GestureRecognizerCollection? _gestureRecognizers;

        /// <summary>
        /// Initializes static members of the <see cref="InputElement"/> class.
        /// </summary>
        static InputElement()
        {
            IsEnabledProperty.Changed.Subscribe(IsEnabledChanged);

            GotFocusEvent.AddClassHandler<InputElement>((x, e) => x.OnGotFocusCore(e));
            LostFocusEvent.AddClassHandler<InputElement>((x, e) => x.OnLostFocusCore(e));
            GettingFocusEvent.AddClassHandler<InputElement>((x, e) => x.OnGettingFocus(e));
            LosingFocusEvent.AddClassHandler<InputElement>((x, e) => x.OnLosingFocus(e));
            KeyDownEvent.AddClassHandler<InputElement>((x, e) => x.OnKeyDown(e));
            KeyUpEvent.AddClassHandler<InputElement>((x, e) => x.OnKeyUp(e));
            TextInputEvent.AddClassHandler<InputElement>((x, e) => x.OnTextInput(e));
            PointerEnteredEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerEnteredCore(e));
            PointerExitedEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerExitedCore(e));
            PointerMovedEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerMoved(e));
            PointerPressedEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerPressed(e));
            PointerReleasedEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerReleased(e));
            PointerCaptureChangingEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerCaptureChanging(e));
            PointerCaptureLostEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerCaptureLost(e));
            PointerWheelChangedEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerWheelChanged(e));

            TappedEvent.AddClassHandler<InputElement>((x, e) => x.OnTapped(e));
            RightTappedEvent.AddClassHandler<InputElement>((x, e) => x.OnRightTapped(e));
            DoubleTappedEvent.AddClassHandler<InputElement>((x, e) => x.OnDoubleTapped(e));
            HoldingEvent.AddClassHandler<InputElement>((x, e) => x.OnHolding(e));

            // Gesture only handlers
            PointerMovedEvent.AddClassHandler<InputElement>((x, e) => x.OnGesturePointerMoved(e), handledEventsToo: true);
            PointerPressedEvent.AddClassHandler<InputElement>((x, e) => x.OnGesturePointerPressed(e), handledEventsToo: true);
            PointerReleasedEvent.AddClassHandler<InputElement>((x, e) => x.OnGesturePointerReleased(e), handledEventsToo: true);
            PointerCaptureLostEvent.AddClassHandler<InputElement>((x, e) => x.OnGesturePointerCaptureLost(e), handledEventsToo: true);


            // Access Key Handling
            AccessKeyHandler.AccessKeyEvent.AddClassHandler<InputElement>((x, e) => x.OnAccessKey(e));
        }

        public InputElement()
        {
            UpdatePseudoClasses(IsFocused, IsPointerOver);
        }

        /// <summary>
        /// Occurs when the control receives focus.
        /// </summary>
        public event EventHandler<GotFocusEventArgs>? GotFocus
        {
            add { AddHandler(GotFocusEvent, value); }
            remove { RemoveHandler(GotFocusEvent, value); }
        }

        /// <summary>
        /// Occurs before the control receives focus.
        /// </summary>
        public event EventHandler<FocusChangingEventArgs>? GettingFocus
        {
            add { AddHandler(GettingFocusEvent, value); }
            remove { RemoveHandler(GettingFocusEvent, value); }
        }

        /// <summary>
        /// Occurs when the control loses focus.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? LostFocus
        {
            add { AddHandler(LostFocusEvent, value); }
            remove { RemoveHandler(LostFocusEvent, value); }
        }

        /// <summary>
        /// Occurs before the control loses focus.
        /// </summary>
        public event EventHandler<FocusChangingEventArgs>? LosingFocus
        {
            add { AddHandler(LosingFocusEvent, value); }
            remove { RemoveHandler(LosingFocusEvent, value); }
        }

        /// <summary>
        /// Occurs when a key is pressed while the control has focus.
        /// </summary>
        public event EventHandler<KeyEventArgs>? KeyDown
        {
            add { AddHandler(KeyDownEvent, value); }
            remove { RemoveHandler(KeyDownEvent, value); }
        }

        /// <summary>
        /// Occurs when a key is released while the control has focus.
        /// </summary>
        public event EventHandler<KeyEventArgs>? KeyUp
        {
            add { AddHandler(KeyUpEvent, value); }
            remove { RemoveHandler(KeyUpEvent, value); }
        }

        /// <summary>
        /// Occurs when a user typed some text while the control has focus.
        /// </summary>
        public event EventHandler<TextInputEventArgs>? TextInput
        {
            add { AddHandler(TextInputEvent, value); }
            remove { RemoveHandler(TextInputEvent, value); }
        }

        /// <summary>
        /// Occurs when an input element gains input focus and input method is looking for the corresponding client
        /// </summary>
        public event EventHandler<TextInputMethodClientRequestedEventArgs>? TextInputMethodClientRequested
        {
            add { AddHandler(TextInputMethodClientRequestedEvent, value); }
            remove { RemoveHandler(TextInputMethodClientRequestedEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer enters the control.
        /// </summary>
        public event EventHandler<PointerEventArgs>? PointerEntered
        {
            add { AddHandler(PointerEnteredEvent, value); }
            remove { RemoveHandler(PointerEnteredEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer leaves the control.
        /// </summary>
        public event EventHandler<PointerEventArgs>? PointerExited
        {
            add { AddHandler(PointerExitedEvent, value); }
            remove { RemoveHandler(PointerExitedEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer moves over the control.
        /// </summary>
        public event EventHandler<PointerEventArgs>? PointerMoved
        {
            add { AddHandler(PointerMovedEvent, value); }
            remove { RemoveHandler(PointerMovedEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer is pressed over the control.
        /// </summary>
        public event EventHandler<PointerPressedEventArgs>? PointerPressed
        {
            add { AddHandler(PointerPressedEvent, value); }
            remove { RemoveHandler(PointerPressedEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer is released over the control.
        /// </summary>
        public event EventHandler<PointerReleasedEventArgs>? PointerReleased
        {
            add { AddHandler(PointerReleasedEvent, value); }
            remove { RemoveHandler(PointerReleasedEvent, value); }
        }

        /// <summary>
        /// Occurs when the control or its child control is about to lose capture,
        /// event will not be triggered for a parent control if capture was transferred to another child of that parent control.
        /// </summary>
        internal event EventHandler<PointerCaptureChangingEventArgs>? PointerCaptureChanging
        {
            add => AddHandler(PointerCaptureChangingEvent, value);
            remove => RemoveHandler(PointerCaptureChangingEvent, value);
        }

        /// <summary>
        /// Occurs when the control or its child control loses the pointer capture for any reason,
        /// event will not be triggered for a parent control if capture was transferred to another child of that parent control.
        /// </summary>
        public event EventHandler<PointerCaptureLostEventArgs>? PointerCaptureLost
        {
            add => AddHandler(PointerCaptureLostEvent, value);
            remove => RemoveHandler(PointerCaptureLostEvent, value);
        }

        /// <summary>
        /// Occurs when the mouse is scrolled over the control.
        /// </summary>
        public event EventHandler<PointerWheelEventArgs>? PointerWheelChanged
        {
            add { AddHandler(PointerWheelChangedEvent, value); }
            remove { RemoveHandler(PointerWheelChangedEvent, value); }
        }

        /// <summary>
        /// Occurs when a tap gesture occurs on the control.
        /// </summary>
        public event EventHandler<TappedEventArgs>? Tapped
        {
            add { AddHandler(TappedEvent, value); }
            remove { RemoveHandler(TappedEvent, value); }
        }

        /// <summary>
        /// Occurs when a right tap gesture occurs on the control.
        /// </summary>
        public event EventHandler<TappedEventArgs>? RightTapped
        {
            add { AddHandler(RightTappedEvent, value); }
            remove { RemoveHandler(RightTappedEvent, value); }
        }

        /// <summary>
        /// Occurs when a hold gesture occurs on the control.
        /// </summary>
        public event EventHandler<HoldingRoutedEventArgs>? Holding
        {
            add { AddHandler(HoldingEvent, value); }
            remove { RemoveHandler(HoldingEvent, value); }
        }

        /// <summary>
        /// Occurs when a double-tap gesture occurs on the control.
        /// </summary>
        public event EventHandler<TappedEventArgs>? DoubleTapped
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
        public Cursor? Cursor
        {
            get { return GetValue(CursorProperty); }
            set { SetValue(CursorProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether keyboard focus is anywhere within the element or its visual tree child elements.
        /// </summary>
        public bool IsKeyboardFocusWithin
        {
            get => _isKeyboardFocusWithin;
            internal set => SetAndRaise(IsKeyboardFocusWithinProperty, ref _isKeyboardFocusWithin, value);
        }

        /// <summary>
        /// Gets a value indicating whether the control is focused.
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
        /// Gets a value indicating whether the pointer is currently over the control.
        /// </summary>
        public bool IsPointerOver
        {
            get { return _isPointerOver; }
            internal set { SetAndRaise(IsPointerOverProperty, ref _isPointerOver, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the control is included in tab navigation.
        /// </summary>
        public bool IsTabStop
        {
            get => GetValue(IsTabStopProperty);
            set => SetValue(IsTabStopProperty, value);
        }

        /// <inheritdoc/>
        public bool IsEffectivelyEnabled
        {
            get => _isEffectivelyEnabled;
            private set
            {
                SetAndRaise(IsEffectivelyEnabledProperty, ref _isEffectivelyEnabled, value);
                PseudoClasses.Set(":disabled", !value);

                if (!IsEffectivelyEnabled && FocusManager.GetFocusManager(this) is { } focusManager
                    && Equals(focusManager.GetFocusedElement(), this))
                {
                    focusManager.ClearFocus();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that determines the order in which elements receive focus when the
        /// user navigates through controls by pressing the Tab key.
        /// </summary>
        public int TabIndex
        {
            get => GetValue(TabIndexProperty);
            set => SetValue(TabIndexProperty, value);
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

        /// <inheritdoc />
        public bool Focus(NavigationMethod method = NavigationMethod.Unspecified, KeyModifiers keyModifiers = KeyModifiers.None)
        {
            return FocusManager.GetFocusManager(this)?.Focus(this, method, keyModifiers) ?? false;
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTreeCore(e);

            if (IsFocused)
            {
                FocusManager.GetFocusManager(e.Root as IInputElement)?.ClearFocusOnElementRemoved(this, e.Parent);
            }

            IsKeyboardFocusWithin = false;
        }

        /// <summary>
        /// This method is used to execute the action on an effective IInputElement when a corresponding access key has been invoked.
        /// By default, the Focus() method is invoked with the NavigationMethod.Tab to indicate a visual focus adorner.
        /// Overwrite this method if other methods or additional functionality is needed when an item should receive the focus.
        /// </summary>
        /// <param name="e">AccessKeyEventArgs are passed on to indicate if there are multiple matches or not.</param>
        protected virtual void OnAccessKey(RoutedEventArgs e)
        {
            Focus(NavigationMethod.Tab);
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTreeCore(e);
            UpdateIsEffectivelyEnabled();
        }

        private void OnGotFocusCore(GotFocusEventArgs e)
        {
            var isFocused = e.Source == this;
            _isFocusVisible = isFocused && (e.NavigationMethod == NavigationMethod.Directional || e.NavigationMethod == NavigationMethod.Tab);
            IsFocused = isFocused;
            OnGotFocus(e);
        }

        protected virtual void OnGettingFocus(FocusChangingEventArgs e)
        {

        }

        protected virtual void OnLosingFocus(FocusChangingEventArgs e)
        {

        }

        /// <summary>
        /// Invoked when an unhandled <see cref="GotFocusEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnGotFocus(GotFocusEventArgs e)
        {
        }

        private void OnLostFocusCore(RoutedEventArgs e)
        {
            _isFocusVisible = false;
            IsFocused = false;
            OnLostFocus(e);
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="LostFocusEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnLostFocus(RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="KeyDownEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnKeyDown(KeyEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="KeyUpEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnKeyUp(KeyEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="TextInputEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnTextInput(TextInputEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="PointerEnteredEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnPointerEntered(PointerEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="PointerExitedEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnPointerExited(PointerEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="PointerMovedEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnPointerMoved(PointerEventArgs e)
        {
        }


        /// <summary>
        /// Invoked when an unhandled <see cref="PointerPressedEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnPointerPressed(PointerPressedEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="PointerReleasedEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnPointerReleased(PointerReleasedEventArgs e)
        {
        }

        private void OnGesturePointerReleased(PointerReleasedEventArgs e)
        {
            if (!e.IsGestureRecognitionSkipped)
                if (_gestureRecognizers?.HandlePointerReleased(e) == true)
                {
                    e.Handled = true;
                }
        }

        private void OnGesturePointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            _gestureRecognizers?.HandleCaptureLost(e.Pointer);
        }

        private void OnGesturePointerPressed(PointerPressedEventArgs e)
        {
            if (!e.IsGestureRecognitionSkipped)
                if (_gestureRecognizers?.HandlePointerPressed(e) == true)
                {
                    e.Handled = true;
                }
        }

        private void OnGesturePointerMoved(PointerEventArgs e)
        {
            if (!e.IsGestureRecognitionSkipped)
                if (_gestureRecognizers?.HandlePointerMoved(e) == true)
                {
                    e.Handled = true;
                }
        }

        /// <summary>
        /// Called when FocusManager get the next TabStop to interact with the focused control.
        /// </summary>
        /// <returns>Next tab stop.</returns>
        protected internal virtual InputElement? GetNextTabStopOverride() => null;

        /// <summary>
        /// Called when FocusManager get the previous TabStop to interact with the focused control.
        /// </summary>
        /// <returns>Previous tab stop.</returns>
        protected internal virtual InputElement? GetPreviousTabStopOverride() => null;

        /// <summary>
        /// Called when FocusManager is looking for the first focusable element from the specified search scope.
        /// </summary>
        /// <returns>First focusable element if available.</returns>
        protected internal virtual InputElement? GetFirstFocusableElementOverride() => null;

        /// <summary>
        /// Called when FocusManager is looking for the last focusable element from the specified search scope.
        /// </summary>
        /// <returns>Last focusable element if available/>.</returns>
        protected internal virtual InputElement? GetLastFocusableElementOverride() => null;

        /// <summary>
        /// Invoked when an unhandled <see cref="PointerCaptureChangingEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        internal virtual void OnPointerCaptureChanging(PointerCaptureChangingEventArgs e)
        {

        }

        /// <summary>
        /// Invoked when an unhandled <see cref="PointerCaptureLostEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {

        }

        internal static bool ProcessTabStop(IInputElement? contentRoot,
            IInputElement? focusedElement,
            IInputElement? candidateTabStopElement,
            bool isReverse,
            bool didCycleFocusAtRootVisual,
            out IInputElement? newTabStop)
        {
            newTabStop = null;
            bool isTabStopOverridden = false;
            bool isCandidateTabStopOverridden = false;
            IInputElement? currentFocusedTarget = focusedElement;
            InputElement? focusedTargetAsIE = focusedElement as InputElement;
            InputElement? candidateTargetAsIE = candidateTabStopElement as InputElement;
            InputElement? newCandidateTargetAsIE = null;
            IInputElement? newCandidateTabStop = null;
            IInputElement? spNewTabStop = null;

            if (focusedTargetAsIE != null)
            {
                isTabStopOverridden = focusedTargetAsIE.ProcessTabStopInternal(candidateTabStopElement, isReverse, didCycleFocusAtRootVisual, out spNewTabStop);
            }

            if (!isTabStopOverridden && candidateTargetAsIE != null)
            {
                isTabStopOverridden = candidateTargetAsIE.ProcessCandidateTabStopInternal(focusedElement, null, isReverse, out spNewTabStop);
            }
            else if (isTabStopOverridden && newTabStop != null)
            {
                newCandidateTargetAsIE = spNewTabStop as InputElement;
                if (newCandidateTargetAsIE != null)
                {
                    isCandidateTabStopOverridden = newCandidateTargetAsIE.ProcessCandidateTabStopInternal(focusedElement, spNewTabStop, isReverse, out newCandidateTabStop);
                }
            }

            if (isCandidateTabStopOverridden)
            {
                if (newCandidateTabStop != null)
                {
                    newTabStop = newCandidateTabStop;
                }

                isTabStopOverridden = true;
            }
            else if (isTabStopOverridden)
            {
                if (newTabStop != null)
                {
                    newTabStop = spNewTabStop;
                }

                isTabStopOverridden = true;
            }

            return isTabStopOverridden;
        }

        private bool ProcessTabStopInternal(IInputElement? candidateTabStopElement,
            bool isReverse,
            bool didCycleFocusAtRootVisual,
            out IInputElement? newTabStop)
        {
            InputElement? current = this;

            newTabStop = null;
            var candidateTabStopOverridden = false;

            while (current != null && !candidateTabStopOverridden)
            {
                candidateTabStopOverridden = current.ProcessTabStopOverride(this,
                    candidateTabStopElement,
                    isReverse,
                    didCycleFocusAtRootVisual,
                    ref newTabStop);

                current = (current as Visual)?.Parent as InputElement;
            }
            return candidateTabStopOverridden;
        }

        private bool ProcessCandidateTabStopInternal(IInputElement? currentTabStop,
            IInputElement? overridenCandidateTabStopElement,
            bool isReverse,
            out IInputElement? newTabStop)
        {
            InputElement? current = this;

            newTabStop = null;
            var candidateTabStopOverridden = false;

            while (current != null && !candidateTabStopOverridden)
            {
                candidateTabStopOverridden = current.ProcessCandidateTabStopOverride(currentTabStop,
                    this,
                    overridenCandidateTabStopElement,
                    isReverse,
                    ref newTabStop);

                current = (current as Visual)?.Parent as InputElement;
            }
            return candidateTabStopOverridden;
        }

        protected internal virtual bool ProcessTabStopOverride(IInputElement? focusedElement,
            IInputElement? candidateTabStopElement,
            bool isReverse,
            bool didCycleFocusAtRootVisual,
            ref IInputElement? newTabStop)
        {
            return false;
        }

        protected internal virtual bool ProcessCandidateTabStopOverride(IInputElement? focusedElement,
            IInputElement? candidateTabStopElement,
            IInputElement? overridenCandidateTabStopElement,
            bool isReverse,
            ref IInputElement? newTabStop)
        {
            return false;
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="PointerWheelChangedEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnPointerWheelChanged(PointerWheelEventArgs e)
        {

        }

        /// <summary>
        /// Invoked when an unhandled <see cref="TappedEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnTapped(TappedEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="RightTappedEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnRightTapped(TappedEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="DoubleTappedEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnDoubleTapped(TappedEventArgs e)
        {
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="HoldingEvent"/> reaches an element in its 
        /// route that is derived from this class. Implement this method to add class handling 
        /// for this event.
        /// </summary>
        /// <param name="e">Data about the event.</param>
        protected virtual void OnHolding(HoldingRoutedEventArgs e)
        {
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsFocusedProperty)
            {
                UpdatePseudoClasses(change.GetNewValue<bool>(), null);
            }
            else if (change.Property == IsPointerOverProperty)
            {
                UpdatePseudoClasses(null, change.GetNewValue<bool>());
            }
            else if (change.Property == IsKeyboardFocusWithinProperty)
            {
                PseudoClasses.Set(":focus-within", change.GetNewValue<bool>());
            }
            else if (change.Property == IsVisibleProperty)
            {
                if (!change.GetNewValue<bool>() && IsKeyboardFocusWithin && FocusManager.GetFocusManager(this) is { } focusManager)
                {
                    if (focusManager.GetFocusedElement() is { } focusedElement && VisualParent != null)
                    {
                        focusManager.ClearFocusOnElementRemoved(focusedElement, VisualParent);
                    }
                    else
                    {
                        focusManager.ClearFocus();
                    }
                }
            }
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
        /// Called before the <see cref="PointerEntered"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void OnPointerEnteredCore(PointerEventArgs e)
        {
            IsPointerOver = true;
            OnPointerEntered(e);
        }

        /// <summary>
        /// Called before the <see cref="PointerExited"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void OnPointerExitedCore(PointerEventArgs e)
        {
            IsPointerOver = false;
            OnPointerExited(e);
        }

        /// <summary>
        /// Updates the <see cref="IsEffectivelyEnabled"/> property based on the parent's
        /// <see cref="IsEffectivelyEnabled"/>.
        /// </summary>
        /// <param name="parent">The parent control.</param>
        private void UpdateIsEffectivelyEnabled(InputElement? parent)
        {
            IsEffectivelyEnabled = IsEnabledCore && (parent?.IsEffectivelyEnabled ?? true);

            // PERF-SENSITIVE: This is called on entire hierarchy and using foreach or LINQ
            // will cause extra allocations and overhead.

            var children = VisualChildren;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < children.Count; ++i)
            {
                var child = children[i] as InputElement;

                child?.UpdateIsEffectivelyEnabled(this);
            }
        }

        private void UpdatePseudoClasses(bool? isFocused, bool? isPointerOver)
        {
            if (isFocused.HasValue)
            {
                PseudoClasses.Set(":focus", isFocused.Value);
                PseudoClasses.Set(":focus-visible", _isFocusVisible);
            }

            if (isPointerOver.HasValue)
            {
                PseudoClasses.Set(":pointerover", isPointerOver.Value);
            }
        }
    }
}
