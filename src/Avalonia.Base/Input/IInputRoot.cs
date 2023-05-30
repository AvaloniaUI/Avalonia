using Avalonia.Metadata;

namespace Avalonia.Input
{
    /// <summary>
    /// Defines the interface for top-level input elements.
    /// </summary>
    [NotClientImplementable]
    public interface IInputRoot : IInputElement
    {
        /// <summary>
        /// Gets or sets the keyboard navigation handler.
        /// </summary>
        IKeyboardNavigationHandler KeyboardNavigationHandler { get; }

        /// <summary>
        /// Gets focus manager of the root.
        /// </summary>
        /// <remarks>
        /// Focus manager can be null only if application wasn't initialized yet.
        /// </remarks>
        IFocusManager? FocusManager { get; }
        
        /// <summary>
        /// Gets or sets the input element that the pointer is currently over.
        /// </summary>
        IInputElement? PointerOverElement { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether access keys are shown in the window.
        /// </summary>
        bool ShowAccessKeys { get; set; }
    }
}
