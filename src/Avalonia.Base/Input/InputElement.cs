using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

#nullable enable

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
        /// Defines the <see cref="AllowFocusWhenDisabled"/> property
        /// </summary>
        public static readonly StyledProperty<bool> AllowFocusWhenDisabledProperty =
            AvaloniaProperty.Register<InputElement, bool>(nameof(AllowFocusWhenDisabled));

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
        /// Defines the <see cref="FocusState"/> property
        /// </summary>
        public static readonly DirectProperty<InputElement, FocusState> FocusStateProperty =
            AvaloniaProperty.RegisterDirect<InputElement, FocusState>(nameof(FocusState), x => x.FocusState);

        /// <summary>
        /// Defines the <see cref="IsTabStop"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsTabStopProperty =
            KeyboardNavigation.IsTabStopProperty.AddOwner<InputElement>();

        /// <summary>
        /// Defines the <see cref="XYFocusKeyboardNavigation"/> property
        /// </summary>
        public static readonly StyledProperty<XYFocusNavigationMode> XYFocusKeyboardNavigationProperty =
            AvaloniaProperty.Register<InputElement, XYFocusNavigationMode>(nameof(XYFocusKeyboardNavigation));

        /// <summary>
        /// Defines the <see cref="XYFocusDown"/> property
        /// </summary>
        public static readonly StyledProperty<IInputElement?> XYFocusDownProperty =
            AvaloniaProperty.Register<InputElement, IInputElement?>(nameof(XYFocusDown));

        /// <summary>
        /// Defines the <see cref="XYFocusDownStrategy"/> property
        /// </summary>
        public static readonly StyledProperty<XYFocusNavigationStrategy> XYFocusDownStrategyProperty =
            AvaloniaProperty.Register<InputElement, XYFocusNavigationStrategy>(nameof(XYFocusDownStrategy));

        /// <summary>
        /// Defines the <see cref="XYFocusLeft"/> property
        /// </summary>
        public static readonly StyledProperty<IInputElement?> XYFocusLeftProperty =
            AvaloniaProperty.Register<InputElement, IInputElement?>(nameof(XYFocusLeft));

        /// <summary>
        /// Defines the <see cref="XYFocusLeftStrategy"/> property
        /// </summary>
        public static readonly StyledProperty<XYFocusNavigationStrategy> XYFocusLeftStrategyProperty =
            AvaloniaProperty.Register<InputElement, XYFocusNavigationStrategy>(nameof(XYFocusLeftStrategy));

        /// <summary>
        /// Defines the <see cref="XYFocusRight"/> property
        /// </summary>
        public static readonly StyledProperty<IInputElement?> XYFocusRightProperty =
            AvaloniaProperty.Register<InputElement, IInputElement?>(nameof(XYFocusRight));

        /// <summary>
        /// Defines the <see cref="XYFocusRightStrategy"/> property
        /// </summary>
        public static readonly StyledProperty<XYFocusNavigationStrategy> XYFocusRightStrategyProperty =
            AvaloniaProperty.Register<InputElement, XYFocusNavigationStrategy>(nameof(XYFocusRightStrategy));

        /// <summary>
        /// Defines the <see cref="XYFocusUp"/> property
        /// </summary>
        public static readonly StyledProperty<IInputElement?> XYFocusUpProperty =
            AvaloniaProperty.Register<InputElement, IInputElement?>(nameof(XYFocusUp));

        /// <summary>
        /// Defines the <see cref="XYFocusUpStrategy"/> property
        /// </summary>
        public static readonly StyledProperty<XYFocusNavigationStrategy> XYFocusUpStrategyProperty =
            AvaloniaProperty.Register<InputElement, XYFocusNavigationStrategy>(nameof(XYFocusUpStrategy));

        /// <summary>
        /// Defines the <see cref="GettingFocus"/> event
        /// </summary>
        public static readonly RoutedEvent<GettingFocusEventArgs> GettingFocusEvent =
            RoutedEvent.Register<InputElement, GettingFocusEventArgs>(nameof(GettingFocus), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="GotFocus"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> GotFocusEvent =
            RoutedEvent.Register<InputElement, RoutedEventArgs>(nameof(GotFocus), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="LosingFocus"/> event
        /// </summary>
        public static readonly RoutedEvent<LosingFocusEventArgs> LosingFocusEvent =
            RoutedEvent.Register<InputElement, LosingFocusEventArgs>(nameof(LosingFocus), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="LostFocus"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> LostFocusEvent =
            RoutedEvent.Register<InputElement, RoutedEventArgs>(nameof(LostFocus), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="NoFocusCandidateFound"/> event
        /// </summary>
        public static readonly RoutedEvent<NoFocusCandidateFoundEventArgs> NoFocusCandidateFoundEvent =
            RoutedEvent.Register<InputElement, NoFocusCandidateFoundEventArgs>(nameof(NoFocusCandidateFoundEvent), RoutingStrategies.Bubble);

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
        /// Defines the <see cref="DoubleTapped"/> event.
        /// </summary>
        public static readonly RoutedEvent<TappedEventArgs> DoubleTappedEvent = Gestures.DoubleTappedEvent;

        private bool _isEffectivelyEnabled = true;
        private bool _isFocused;
        private bool _isKeyboardFocusWithin;
        private bool _isFocusVisible;
        private bool _isPointerOver;
        private GestureRecognizerCollection? _gestureRecognizers;
        private FocusState _focusState;

        /// <summary>
        /// Initializes static members of the <see cref="InputElement"/> class.
        /// </summary>
        static InputElement()
        {
            IsEnabledProperty.Changed.Subscribe(IsEnabledChanged);

            GettingFocusEvent.AddClassHandler<InputElement>((x, e) => x.OnGettingFocus(e));
            LosingFocusEvent.AddClassHandler<InputElement>((x, e) => x.OnLosingFocus(e));
            GotFocusEvent.AddClassHandler<InputElement>((x, e) => x.OnGotFocus(e));
            LostFocusEvent.AddClassHandler<InputElement>((x, e) => x.OnLostFocus(e));
            KeyDownEvent.AddClassHandler<InputElement>((x, e) => x.OnKeyDown(e));
            KeyUpEvent.AddClassHandler<InputElement>((x, e) => x.OnKeyUp(e));
            TextInputEvent.AddClassHandler<InputElement>((x, e) => x.OnTextInput(e));
            PointerEnterEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerEnterCore(e));
            PointerLeaveEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerLeaveCore(e));
            PointerMovedEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerMoved(e));
            PointerPressedEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerPressed(e));
            PointerReleasedEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerReleased(e));
            PointerCaptureLostEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerCaptureLost(e));
            PointerWheelChangedEvent.AddClassHandler<InputElement>((x, e) => x.OnPointerWheelChanged(e));
        }

        public InputElement()
        {
            UpdatePseudoClasses(IsFocused, IsPointerOver);
        }

        /// <inheritdoc/>
        public event EventHandler<GettingFocusEventArgs>? GettingFocus
        {
            add => AddHandler(GettingFocusEvent, value);
            remove => RemoveHandler(GettingFocusEvent, value);
        }

        /// <summary>
        /// Occurs when the control receives focus.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? GotFocus
        {
            add { AddHandler(GotFocusEvent, value); }
            remove { RemoveHandler(GotFocusEvent, value); }
        }

        /// <summary>
        /// Occurs before the control loses focus
        /// </summary>
        public event EventHandler<LosingFocusEventArgs>? LosingFocus
        {
            add => AddHandler(LosingFocusEvent, value);
            remove => RemoveHandler(LosingFocusEvent, value);
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
        /// Occurs when the user attempts to change focus (via keyboard) but focus is unable
        /// to move because no focus candidate is found in the direction of movement
        /// </summary>
        public event EventHandler<NoFocusCandidateFoundEventArgs>? NoFocusCandidateFound
        {
            add => AddHandler(NoFocusCandidateFoundEvent, value);
            remove => RemoveHandler(NoFocusCandidateFoundEvent, value);
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
        public event EventHandler<PointerEventArgs>? PointerEnter
        {
            add { AddHandler(PointerEnterEvent, value); }
            remove { RemoveHandler(PointerEnterEvent, value); }
        }

        /// <summary>
        /// Occurs when the pointer leaves the control.
        /// </summary>
        public event EventHandler<PointerEventArgs>? PointerLeave
        {
            add { AddHandler(PointerLeaveEvent, value); }
            remove { RemoveHandler(PointerLeaveEvent, value); }
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
        /// Occurs when the control or its child control loses the pointer capture for any reason,
        /// event will not be triggered for a parent control if capture was transferred to another child of that parent control
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

        /// <inheritdoc/>
        public bool AllowFocusWhenDisabled
        {
            get => GetValue(AllowFocusWhenDisabledProperty);
            set => SetValue(AllowFocusWhenDisabledProperty, value);
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

        /// <inheritdoc/>
        public FocusState FocusState
        {
            get => _focusState;
            internal set => SetAndRaise(FocusStateProperty, ref _focusState, value);
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

        /// <inheritdoc/>
        public XYFocusNavigationMode XYFocusKeyboardNavigation
        {
            get => GetValue(XYFocusKeyboardNavigationProperty);
            set => SetValue(XYFocusKeyboardNavigationProperty, value);
        }

        /// <inheritdoc/>
        public IInputElement? XYFocusDown
        {
            get => GetValue(XYFocusDownProperty);
            set => SetValue(XYFocusDownProperty, value);
        }

        /// <inheritdoc/>
        public XYFocusNavigationStrategy XYFocusDownStrategy
        {
            get => GetValue(XYFocusDownStrategyProperty);
            set => SetValue(XYFocusDownStrategyProperty, value);
        }

        /// <inheritdoc/>
        public IInputElement? XYFocusLeft
        {
            get => GetValue(XYFocusLeftProperty);
            set => SetValue(XYFocusLeftProperty, value);
        }

        /// <inheritdoc/>
        public XYFocusNavigationStrategy XYFocusLeftStrategy
        {
            get => GetValue(XYFocusLeftStrategyProperty);
            set => SetValue(XYFocusLeftStrategyProperty, value);
        }

        /// <inheritdoc/>
        public IInputElement? XYFocusRight
        {
            get => GetValue(XYFocusRightProperty);
            set => SetValue(XYFocusRightProperty, value);
        }

        /// <inheritdoc/>
        public XYFocusNavigationStrategy XYFocusRightStrategy
        {
            get => GetValue(XYFocusRightStrategyProperty);
            set => SetValue(XYFocusRightStrategyProperty, value);
        }

        /// <inheritdoc/>
        public IInputElement? XYFocusUp
        {
            get => GetValue(XYFocusUpProperty);
            set => SetValue(XYFocusUpProperty, value);
        }

        /// <inheritdoc/>
        public XYFocusNavigationStrategy XYFocusUpStrategy
        {
            get => GetValue(XYFocusUpStrategyProperty);
            set => SetValue(XYFocusUpStrategyProperty, value);
        }

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
            Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Focuses the control with the given <see cref="FocusState"/>
        /// </summary>
        public void Focus(FocusState focusState)
        {
            var root = VisualRoot as IInputRoot ?? this.FindAncestorOfType<IInputRoot>();
            if (root == null)
            {
                throw new InvalidOperationException("Unable to find IInputRoot or FocusManager from current element.");
            }

            root.FocusManager.SetFocusedElement(this, focusState);
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTreeCore(e);

            if (IsFocused)
            {
                // TODO_FOCUS
                //FocusManager.Instance?.Focus(null);
            }
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTreeCore(e);
            UpdateIsEffectivelyEnabled();
        }

        protected virtual void OnGettingFocus(GettingFocusEventArgs e)
        {

        }

        /// <summary>
        /// Called before the <see cref="GotFocus"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnGotFocus(RoutedEventArgs e)
        {
            var isFocused = e.Source == this;
            _isFocusVisible = isFocused && FocusState == FocusState.Keyboard;
            IsFocused = isFocused;
        }

        protected virtual void OnLosingFocus(LosingFocusEventArgs e)
        {

        }

        /// <summary>
        /// Called before the <see cref="LostFocus"/> event occurs.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnLostFocus(RoutedEventArgs e)
        {
            _isFocusVisible = false;
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
            // TODO_FOCUS: Adjust focus if lost focus
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
