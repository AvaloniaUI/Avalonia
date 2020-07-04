namespace Avalonia.Controls
{
    /// <summary>
    /// Interface implemented by controls that act as controllers for an
    /// <see cref="IVirtualizingPanel"/>.
    /// </summary>
    public interface IVirtualizingController
    {
        /// <summary>
        /// Called when the <see cref="IVirtualizingPanel"/>'s controls should be updated.
        /// </summary>
        /// <remarks>
        /// The controller should respond to this method being called by either adding
        /// children up until <see cref="IVirtualizingPanel.IsFull"/> becomes true or
        /// removing <see cref="IVirtualizingPanel.OverflowCount"/> controls.
        /// </remarks>
        void UpdateControls();
    }
}
