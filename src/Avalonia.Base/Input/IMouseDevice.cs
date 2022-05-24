using System;
using Avalonia.Metadata;

namespace Avalonia.Input
{
    /// <summary>
    /// Represents a mouse device.
    /// </summary>
    [NotClientImplementable]
    public interface IMouseDevice : IPointerDevice
    {
        /// <summary>
        /// Gets the mouse position, in screen coordinates.
        /// </summary>
        [Obsolete("Use PointerEventArgs.GetPosition")]
        PixelPoint Position { get; }

        [Obsolete]
        void TopLevelClosed(IInputRoot root);

        [Obsolete]
        void SceneInvalidated(IInputRoot root, Rect rect);
    }
}
