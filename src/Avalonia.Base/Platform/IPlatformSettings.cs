using System;
using Avalonia.Input;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IPlatformSettings
    {
        /// <summary>
        /// The size of the rectangle around the location of a pointer down that a pointer up
        /// must occur within in order to register a tap gesture, in device-independent pixels.
        /// </summary>
        /// <param name="type">The pointer type.</param>
        Size GetTapSize(PointerType type);

        /// <summary>
        /// The size of the rectangle around the location of a pointer down that a pointer up
        /// must occur within in order to register a double-tap gesture, in device-independent
        /// pixels.
        /// </summary>
        /// <param name="type">The pointer type.</param>
        Size GetDoubleTapSize(PointerType type);

        /// <summary>
        /// Gets the maximum time that may occur between the first and second click of a double-
        /// tap gesture.
        /// </summary>
        TimeSpan GetDoubleTapTime(PointerType type);

        TimeSpan HoldWaitDuration { get; set; }
    }
}
