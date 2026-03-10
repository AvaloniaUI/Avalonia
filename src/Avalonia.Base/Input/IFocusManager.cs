using Avalonia.Metadata;

namespace Avalonia.Input
{
    /// <summary>
    /// Manages focus for the application.
    /// </summary>
    [NotClientImplementable]
    public interface IFocusManager
    {
        /// <summary>
        /// Gets the currently focused <see cref="IInputElement"/>.
        /// </summary>
        IInputElement? GetFocusedElement();

        /// <summary>
        /// Focuses a control.
        /// </summary>
        /// <param name="control">The control to focus.</param>
        /// <param name="method">The method by which focus was changed.</param>
        /// <param name="keyModifiers">Any key modifiers active at the time of focus.</param>
        bool Focus(
            IInputElement? control,
            NavigationMethod method = NavigationMethod.Unspecified,
            KeyModifiers keyModifiers = KeyModifiers.None);

        /// <summary>
        /// Attempts to change focus from the element with focus to the next focusable element in the specified direction.
        /// </summary>
        /// <param name="direction">The direction to traverse (in tab order).</param>
        /// <returns>true if focus moved; otherwise, false.</returns>
        bool TryMoveFocus(NavigationDirection direction);

        /// <summary>
        /// Attempts to change focus from the element with focus to the next focusable element in the specified direction, using the specified navigation options.
        /// </summary>
        /// <param name="direction">The direction to traverse (in tab order).</param>
        /// <param name="options">The options to help identify the next element to receive focus.</param>
        /// <returns>true if focus moved; otherwise, false.</returns>
        bool TryMoveFocus(NavigationDirection direction, FindNextElementOptions options);

        /// <summary>
        /// Retrieves the first element that can receive focus.
        /// </summary>
        /// <returns>The first focusable element.</returns>
        IInputElement? FindFirstFocusableElement();

        /// <summary>
        /// Retrieves the last element that can receive focus.
        /// </summary>
        /// <returns>The last focusable element.</returns>
        IInputElement? FindLastFocusableElement();

        /// <summary>
        /// Retrieves the element that should receive focus based on the specified navigation direction.
        /// </summary>
        /// <param name="direction">The direction that focus moves from element to element.</param>
        /// <returns>The next element to receive focus, if any.</returns>
        IInputElement? FindNextElement(NavigationDirection direction);

        /// <summary>
        /// Retrieves the element that should receive focus based on the specified navigation direction.
        /// </summary>
        /// <param name="direction">
        /// The direction that focus moves from element to element.
        /// Must be one of <see cref="NavigationDirection.Left"/>, <see cref="NavigationDirection.Right"/>,
        /// <see cref="NavigationDirection.Up"/> and <see cref="NavigationDirection.Down"/>.
        /// </param>
        /// <param name="options">The options to help identify the next element to receive focus.</param>
        /// <returns>The next element to receive focus, if any.</returns>
        IInputElement? FindNextElement(NavigationDirection direction, FindNextElementOptions options);
    }
}
