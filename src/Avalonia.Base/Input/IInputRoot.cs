using Avalonia.Input.TextInput;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Input
{
    /// <summary>
    /// Defines the interface for top-level input elements.
    /// </summary>
    [PrivateApi]
    public interface IInputRoot
    {
        /// <summary>
        /// Gets focus manager of the root.
        /// </summary>
        /// <remarks>
        /// Focus manager can be null only if window wasn't initialized yet.
        /// </remarks>
        public IFocusManager? FocusManager { get; }
        
        /// <summary>
        /// Gets or sets the input element that the pointer is currently over.
        /// </summary>
        internal IInputElement? PointerOverElement { get; set; }
        
        internal ITextInputMethodImpl? InputMethod { get; }
        
        internal InputElement RootElement { get; }
        
        // HACK: This is a temporary hack for "default focus" concept. 
        // If nothing is focused we send keyboard events to Window. Since for now we always
        // control PresentationSource, we simply pass the TopLevel as a separate parameter there.
        internal InputElement FocusRoot { get; }
    }
}
