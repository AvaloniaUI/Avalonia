using System;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    /// The <see cref="IPlatformSettings"/> interface represents a contract for accessing platform-specific settings and information.
    /// Some of these settings might be changed by used globally in the OS in runtime.
    /// </summary>
    [NotClientImplementable]
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

        /// <summary>
        /// Holding duration between pointer press and when event is fired.
        /// </summary>
        TimeSpan HoldWaitDuration { get; }
        
        /// <summary>
        /// Get a configuration for platform-specific hotkeys in an Avalonia application.
        /// </summary>
        PlatformHotkeyConfiguration HotkeyConfiguration { get; }
        
        /// <summary>
        /// Gets current system color values including dark mode and accent colors.
        /// </summary>
        PlatformColorValues GetColorValues();

        /// <summary>
        /// Raises when current system color values are changed. Including changing of a dark mode and accent colors.
        /// </summary>
        event EventHandler<PlatformColorValues>? ColorValuesChanged;
    }
}
