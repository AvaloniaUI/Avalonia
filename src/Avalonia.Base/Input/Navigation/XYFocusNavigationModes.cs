using System;

namespace Avalonia.Input;

/// <summary>
/// Specifies the 2D directional navigation behavior when using different key devices.
/// </summary>
/// <remarks>
/// See <see cref="KeyDeviceType"/>.
/// </remarks>
[Flags]
public enum XYFocusNavigationModes
{
    /// <summary>
    /// Any key device XY navigation is disabled.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Keyboard arrow keys can be used for 2D directional navigation.
    /// </summary>
    Keyboard = 1,

    /// <summary>
    /// Gamepad controller DPad keys can be used for 2D directional navigation.
    /// </summary>
    Gamepad = 2,

    /// <summary>
    /// Remote controller DPad keys can be used for 2D directional navigation.
    /// </summary>
    Remote = 4,

    /// <summary>
    /// All key device XY navigation is disabled.
    /// </summary>
    Enabled = Gamepad | Remote | Keyboard
}
