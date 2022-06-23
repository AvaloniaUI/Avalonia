using Avalonia.Metadata;

namespace Avalonia.Input
{
    /// <summary>
    /// Defines the interface for classes that handle keyboard navigation for a window.
    /// </summary>
    [Unstable]
    public interface IKeyboardNavigationHandler
    {
        /// <summary>
        /// Sets the owner of the keyboard navigation handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        void SetOwner(IInputRoot owner);
    }
}
