using System;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Avalonia.Input
{
    /// <summary>
    /// Defines input-related functionality for a control.
    /// </summary>
    [NotClientImplementable]
    public interface IInputElement
    {
        /// <summary>
        /// Occurs when the control receives focus.
        /// </summary>
        event EventHandler<GotFocusEventArgs>? GotFocus;

        /// <summary>
        /// Occurs when the control loses focus.
        /// </summary>
        event EventHandler<RoutedEventArgs>? LostFocus;

        /// <summary>
        /// Occurs when a key is pressed while the control has focus.
        /// </summary>
        event EventHandler<KeyEventArgs>? KeyDown;

        /// <summary>
        /// Occurs when a key is released while the control has focus.
        /// </summary>
        event EventHandler<KeyEventArgs>? KeyUp;

        /// <summary>
        /// Occurs when a user typed some text while the control has focus.
        /// </summary>
        event EventHandler<TextInputEventArgs>? TextInput;

        /// <summary>
        /// Occurs when the pointer enters the control.
        /// </summary>
        event EventHandler<PointerEventArgs>? PointerEntered;

        /// <summary>
        /// Occurs when the pointer leaves the control.
        /// </summary>
        event EventHandler<PointerEventArgs>? PointerExited;

        /// <summary>
        /// Occurs when the pointer is pressed over the control.
        /// </summary>
        event EventHandler<PointerPressedEventArgs>? PointerPressed;

        /// <summary>
        /// Occurs when the pointer moves over the control.
        /// </summary>
        event EventHandler<PointerEventArgs>? PointerMoved;

        /// <summary>
        /// Occurs when the pointer is released over the control.
        /// </summary>
        event EventHandler<PointerReleasedEventArgs>? PointerReleased;

        /// <summary>
        /// Occurs when the mouse wheel is scrolled over the control.
        /// </summary>
        event EventHandler<PointerWheelEventArgs>? PointerWheelChanged;

        /// <summary>
        /// Gets or sets a value indicating whether the control can receive keyboard focus.
        /// </summary>
        bool Focusable { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the control is enabled for user interaction.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets or sets the associated mouse cursor.
        /// </summary>
        Cursor? Cursor { get; }

        /// <summary>
        /// Gets a value indicating whether this control and all its parents are enabled.
        /// </summary>
        /// <remarks>
        /// The <see cref="IsEnabled"/> property is used to toggle the enabled state for individual
        /// controls. The <see cref="IsEffectivelyEnabled"/> property takes into account the
        /// <see cref="IsEnabled"/> value of this control and its parent controls.
        /// </remarks>
        bool IsEffectivelyEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether this control and all its parents are visible.
        /// </summary>
        bool IsEffectivelyVisible { get; }

        /// <summary>
        /// Gets a value indicating whether keyboard focus is anywhere within the element or its visual tree child elements.
        /// </summary>
        bool IsKeyboardFocusWithin { get; }

        /// <summary>
        /// Gets a value indicating whether the control is focused.
        /// </summary>
        bool IsFocused { get; }

        /// <summary>
        /// Gets a value indicating whether the control is considered for hit testing.
        /// </summary>
        bool IsHitTestVisible { get; }

        /// <summary>
        /// Gets a value indicating whether the pointer is currently over the control.
        /// </summary>
        bool IsPointerOver { get; }

        /// <summary>
        /// Focuses the control.
        /// </summary>
        /// <param name="method">The method by which focus was changed.</param>
        /// <param name="keyModifiers">Any key modifiers active at the time of focus.</param>
        bool Focus(NavigationMethod method = NavigationMethod.Unspecified, KeyModifiers keyModifiers = KeyModifiers.None);

        /// <summary>
        /// Gets the key bindings for the element.
        /// </summary>
        List<KeyBinding> KeyBindings { get; }

        /// <summary>
        /// Adds a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        /// <returns>A disposable that terminates the event subscription.</returns>
        void AddHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false);

        /// <summary>
        /// Removes a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        void RemoveHandler(RoutedEvent routedEvent, Delegate handler);

        /// <summary>
        /// Raises a routed event.
        /// </summary>
        /// <param name="e">The event args.</param>
        void RaiseEvent(RoutedEventArgs e);
    }
}
