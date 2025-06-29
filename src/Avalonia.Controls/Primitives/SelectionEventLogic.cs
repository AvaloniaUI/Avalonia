using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Primitives;

public enum InputSelectionTrigger
{
    /// <summary>
    /// Do not select in response to this input.
    /// </summary>
    None,
    /// <summary>
    /// Select when this input begins.
    /// </summary>
    Press,
    /// <summary>
    /// Select when this input ends.
    /// </summary>
    Release,
}

/// <summary>
/// Defines standard logic for selecting items via user input. Behaviour differs between input devices.
/// </summary>
public static class SelectionEventLogic
{
    /// <summary>
    /// Analyses an input event received by a selectable element, and determines whether the action should trigger selection on press, on release, or not at all.
    /// </summary>
    /// <remarks>
    /// While this method could also check whether the event actually was a press or release and return <see cref="bool"/>, it 
    /// instead provides a more detailed result which can be better integrated into the caller's selection logic.
    /// </remarks>
    /// <param name="selectable">The selectable element which is processing the event.</param>
    /// <param name="eventArgs">The event to analyse.</param>
    public static InputSelectionTrigger EventSelectionTrigger(InputElement selectable, PointerEventArgs eventArgs)
    {
        if (!new Rect(selectable.Bounds.Size).Contains(eventArgs.GetPosition(selectable)))
        {
            return InputSelectionTrigger.None; // don't select if the pointer has moved away from the element since being pressed
        }

        return eventArgs switch
        {
            // Only select for left/right button events
            { Properties.PointerUpdateKind: not (PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.RightButtonPressed or
                PointerUpdateKind.LeftButtonReleased or PointerUpdateKind.RightButtonReleased) } => InputSelectionTrigger.None,

            // Select on mouse press, unless the mouse can generate gestures
            { Pointer.Type: PointerType.Mouse } => Gestures.GetIsHoldWithMouseEnabled(selectable) ? InputSelectionTrigger.Release : InputSelectionTrigger.Press,

            // Pen "right clicks" are used for context menus, and gestures are only processed for primary input
            { Pointer.Type: PointerType.Pen, Properties.PointerUpdateKind: PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased } => InputSelectionTrigger.Press,
            // For all other pen input, select on release
            { Pointer.Type: PointerType.Pen } => InputSelectionTrigger.Release,

            // Select on touch release
            { Pointer.Type: PointerType.Touch } => InputSelectionTrigger.Release,

            // Don't select in any other case
            _ => InputSelectionTrigger.None,
        };
    }

    /// <inheritdoc cref="EventSelectionTrigger(InputElement, PointerEventArgs)"/>
    public static InputSelectionTrigger EventSelectionTrigger(InputElement selectable, KeyEventArgs eventArgs)
    {
        // Only accept space/enter key presses directly from the selectable
        return eventArgs.Source == selectable && eventArgs.Key is Key.Space or Key.Enter ? InputSelectionTrigger.Press : InputSelectionTrigger.None;
    }

    /// <summary>
    /// Analyses an input event received by a selectable element, and determines whether the action should trigger range selection.
    /// </summary>
    /// <param name="selectable">The selectable element which is processing the event.</param>
    /// <param name="eventArgs">The event to analyse.</param>
    /// <seealso cref="PlatformHotkeyConfiguration.SelectionModifiers"/>
    public static bool HasRangeSelectionModifier(InputElement selectable, RoutedEventArgs eventArgs) => HasModifiers(eventArgs, Hotkeys(selectable)?.SelectionModifiers);

    /// <summary>
    /// Analyses an input event received by a selectable element, and determines whether the action should trigger toggle selection.
    /// </summary>
    /// <param name="selectable">The selectable element which is processing the event.</param>
    /// <param name="eventArgs">The event to analyse.</param>
    /// <seealso cref="PlatformHotkeyConfiguration.CommandModifiers"/>
    public static bool HasToggleSelectionModifier(InputElement selectable, RoutedEventArgs eventArgs) => HasModifiers(eventArgs, Hotkeys(selectable)?.CommandModifiers);

    private static PlatformHotkeyConfiguration? Hotkeys(InputElement element) =>
        (TopLevel.GetTopLevel(element)?.PlatformSettings ?? Application.Current?.PlatformSettings)?.HotkeyConfiguration;

    private static bool HasModifiers(RoutedEventArgs eventArgs, KeyModifiers? modifiers) =>
        modifiers != null && eventArgs is IKeyModifiersEventArgs { KeyModifiers: { } eventModifiers } && eventModifiers.HasAllFlags(modifiers.Value);
}
