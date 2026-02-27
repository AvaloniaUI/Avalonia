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
        // It's also currently used by automation since we have special WindowAutomationPeer which needs to target the
        // window itself
        public InputElement FocusRoot { get; }
        
        /// <summary>
        /// Performs a hit-test for chrome/decoration elements at the given position.
        /// </summary>
        /// <param name="point">The point in root-relative coordinates.</param>
        /// <returns>
        /// <c>null</c> if no chrome element was hit (no chrome involvement at this point).
        /// <see cref="ElementRole.None"/> if an interactive chrome element was hit (e.g., caption button) —
        /// the platform should redirect non-client input to regular client input.
        /// Any other <see cref="ElementRole"/> value indicates a specific non-client role (titlebar, resize grip, etc.).
        /// </returns>
        internal WindowDecorationsElementRole? HitTestChromeElement(Point point) => null;
    }
}
