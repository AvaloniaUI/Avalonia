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
        /// <param name="element">The control to focus.</param>
        /// <param name="method">The method by which focus was changed.</param>
        /// <param name="keyModifiers">Any key modifiers active at the time of focus.</param>
        /// <returns><c>true</c> if the focus moved to a control; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If <paramref name="element"/> is null, this method tries to clear the focus. However, it is not advised.
        /// For a better user experience, focus should be moved to another element when possible.
        ///
        /// When this method returns <c>true</c>, the focus has been moved to <paramref name="element"/>.
        /// When this method returns <c>false</c>, the focus may have been canceled or redirected to another element.
        /// </remarks>
        bool Focus(
            IInputElement? element,
            NavigationMethod method = NavigationMethod.Unspecified,
            KeyModifiers keyModifiers = KeyModifiers.None);

        /// <summary>
        /// Attempts to change focus from the element with focus to the next focusable element in the specified direction.
        /// </summary>
        /// <param name="direction">
        /// The direction that focus moves from element to element.
        /// Must be one of <see cref="NavigationDirection.Next"/>, <see cref="NavigationDirection.Previous"/>,
        /// <see cref="NavigationDirection.Left"/>, <see cref="NavigationDirection.Right"/>,
        /// <see cref="NavigationDirection.Up"/> and <see cref="NavigationDirection.Down"/>.
        /// </param>
        /// <param name="options">The options to help identify the next element to receive focus.</param>
        /// <returns>true if focus moved; otherwise, false.</returns>
        bool TryMoveFocus(NavigationDirection direction, FindNextElementOptions? options = null);

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
        /// <param name="direction">
        /// The direction that focus moves from element to element.
        /// Must be one of <see cref="NavigationDirection.Next"/>, <see cref="NavigationDirection.Previous"/>,
        /// <see cref="NavigationDirection.Left"/>, <see cref="NavigationDirection.Right"/>,
        /// <see cref="NavigationDirection.Up"/> and <see cref="NavigationDirection.Down"/>.
        /// </param>
        /// <param name="options">The options to help identify the next element to receive focus.</param>
        /// <returns>The next element to receive focus, if any.</returns>
        IInputElement? FindNextElement(NavigationDirection direction, FindNextElementOptions? options = null);
    }
}
