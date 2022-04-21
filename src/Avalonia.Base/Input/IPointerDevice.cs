using System;
using Avalonia.VisualTree;
using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    public interface IPointerDevice : IInputDevice
    {
        /// <inheritdoc cref="IPointer.Captured" />
        [Obsolete("Use IPointer")]
        IInputElement? Captured { get; }

        /// <inheritdoc cref="IPointer.Capture(IInputElement?)" />
        [Obsolete("Use IPointer")]
        void Capture(IInputElement? control);

        /// <inheritdoc cref="PointerEventArgs.GetPosition(IVisual?)" />
        [Obsolete("Use PointerEventArgs.GetPosition")]
        Point GetPosition(IVisual relativeTo);

        /// <summary>
        /// Gets a pointer for specific event args.
        /// </summary>
        /// <remarks>
        /// If pointer doesn't exist or wasn't yet created this method will return null.
        /// </remarks>
        /// <param name="ev">Raw pointer event args associated with the pointer.</param>
        /// <returns>The pointer.</returns>
        IPointer? TryGetPointer(RawPointerEventArgs ev);
    }
}
