using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Primitives;

/// <summary>
/// Defines standard logic for selecting items via user input. Behaviour differs between input devices.
/// </summary>
public static class ItemSelectionEventTriggers
{
    /// <summary>
    /// Analyses an input event received by a selectable element, and determines whether the action should trigger selection on press, on release, or not at all.
    /// </summary>
    /// <param name="selectable">The selectable element which is processing the event.</param>
    /// <param name="eventArgs">The event to analyse.</param>
    public static bool ShouldTriggerSelection(Visual selectable, PointerEventArgs eventArgs)
    {
        if (!IsPointerEventWithinBounds(selectable, eventArgs))
        {
            return false; // don't select if the pointer has moved away from the element since being pressed
        }

        return eventArgs switch
        {
            // Only select for left/right button events
            {
                Properties.PointerUpdateKind: not (PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.RightButtonPressed or
                PointerUpdateKind.LeftButtonReleased or PointerUpdateKind.RightButtonReleased)
            } => false,

            // Select on mouse press, unless the mouse can generate gestures
            { Pointer.Type: PointerType.Mouse } => eventArgs.RoutedEvent == (Gestures.GetIsHoldWithMouseEnabled(selectable) ?
                InputElement.PointerReleasedEvent : (RoutedEvent)InputElement.PointerPressedEvent),

            // Pen "right clicks" are used for context menus, and gestures are only processed for primary input
            { Pointer.Type: PointerType.Pen, Properties.PointerUpdateKind: PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased } =>
                eventArgs.RoutedEvent == InputElement.PointerPressedEvent,

            // For all other pen input, select on release
            { Pointer.Type: PointerType.Pen } => eventArgs.RoutedEvent == InputElement.PointerReleasedEvent,

            // Select on touch release
            { Pointer.Type: PointerType.Touch } => eventArgs.RoutedEvent == InputElement.PointerReleasedEvent,

            // Don't select in any other case
            _ => false,
        };
    }

    internal static bool IsPointerEventWithinBounds(Visual selectable, PointerEventArgs eventArgs) =>
        new Rect(selectable.Bounds.Size).Contains(eventArgs.GetPosition(selectable));

    /// <inheritdoc cref="ShouldTriggerSelection(Visual, PointerEventArgs)"/>
    public static bool ShouldTriggerSelection(Visual selectable, KeyEventArgs eventArgs)
    {
        // Only accept space/enter key presses directly from the selectable, otherwise key input can become unpredictable
        return eventArgs.Source == selectable && eventArgs.Key is Key.Space or Key.Enter ? eventArgs.RoutedEvent == InputElement.KeyDownEvent : false;
    }

    /// <summary>
    /// Analyses an input event received by a selectable element, and determines whether the action should trigger range selection.
    /// </summary>
    /// <param name="selectable">The selectable element which is processing the event.</param>
    /// <param name="eventArgs">The event to analyse.</param>
    /// <seealso cref="PlatformHotkeyConfiguration.SelectionModifiers"/>
    public static bool HasRangeSelectionModifier(Visual selectable, RoutedEventArgs eventArgs) => HasModifiers(eventArgs, Hotkeys(selectable)?.SelectionModifiers);

    /// <summary>
    /// Analyses an input event received by a selectable element, and determines whether the action should trigger toggle selection.
    /// </summary>
    /// <param name="selectable">The selectable element which is processing the event.</param>
    /// <param name="eventArgs">The event to analyse.</param>
    /// <seealso cref="PlatformHotkeyConfiguration.CommandModifiers"/>
    public static bool HasToggleSelectionModifier(Visual selectable, RoutedEventArgs eventArgs) => HasModifiers(eventArgs, Hotkeys(selectable)?.CommandModifiers);

    private static PlatformHotkeyConfiguration? Hotkeys(Visual element) =>
        (TopLevel.GetTopLevel(element)?.PlatformSettings ?? Application.Current?.PlatformSettings)?.HotkeyConfiguration;

    private static bool HasModifiers(RoutedEventArgs eventArgs, KeyModifiers? modifiers) =>
        modifiers != null && eventArgs is IKeyModifiersEventArgs { KeyModifiers: { } eventModifiers } && eventModifiers.HasAllFlags(modifiers.Value);
}
