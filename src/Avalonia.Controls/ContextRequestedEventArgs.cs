using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides event data for the ContextRequested event.
    /// </summary>
    public class ContextRequestedEventArgs : RoutedEventArgs
    {
        private readonly PointerEventArgs? _pointerEventArgs;

        /// <summary>
        /// Initializes a new instance of the ContextRequestedEventArgs class.
        /// </summary>
        public ContextRequestedEventArgs()
            : base(Control.ContextRequestedEvent)
        {

        }

        /// <inheritdoc cref="ContextRequestedEventArgs()" />
        public ContextRequestedEventArgs(PointerEventArgs pointerEventArgs)
            : this()
        {
            _pointerEventArgs = pointerEventArgs;
        }

        /// <summary>
        /// Gets the x- and y-coordinates of the pointer position, optionally evaluated against a coordinate origin of a supplied <see cref="Control"/>.
        /// </summary>
        /// <param name="relativeTo">
        /// Any <see cref="Control"/>-derived object that is connected to the same object tree.
        /// To specify the object relative to the overall coordinate system, use a relativeTo  value of null.
        /// </param>
        /// <param name="point">
        /// A <see cref="Point"/> that represents the current x- and y-coordinates of the mouse pointer position.
        /// If null was passed as relativeTo, this coordinate is for the overall window.
        /// If a relativeTo value other than null was passed, this coordinate is relative to the object referenced by relativeTo.
        /// </param>
        /// <returns>
        /// true if the context request was initiated by a pointer device; otherwise, false.
        /// </returns>
        public bool TryGetPosition(Control? relativeTo, out Point point)
        {
            if (_pointerEventArgs is null)
            {
                point = default;
                return false;
            }

            point = _pointerEventArgs.GetPosition(relativeTo);
            return true;
        }
    }
}
