using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Input;

/// <summary>
/// Enumerates key device types.
/// </summary>
public enum KeyDeviceType
{
    /// <summary>
    /// The input device is a keyboard.
    /// </summary>
    Keyboard,

    /// <summary>
    /// The input device is a gamepad.
    /// </summary>
    Gamepad,

    /// <summary>
    /// The input device is a remote control.
    /// </summary>
    Remote
}
