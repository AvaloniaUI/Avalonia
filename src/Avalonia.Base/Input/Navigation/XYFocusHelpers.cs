using System;

namespace Avalonia.Input;

internal static class XYFocusHelpers
{
    internal static bool IsAllowedXYNavigationMode(this InputElement visual, KeyDeviceType? keyDeviceType)
    {
        return IsAllowedXYNavigationMode(XYFocus.GetNavigationModes(visual), keyDeviceType);
    }

    private static bool IsAllowedXYNavigationMode(XYFocusNavigationModes modes, KeyDeviceType? keyDeviceType)
    {
        return keyDeviceType switch
        {
            null => !modes.Equals(XYFocusNavigationModes.Disabled), // programmatic input, allow any subtree except Disabled.
            KeyDeviceType.Keyboard => modes.HasFlag(XYFocusNavigationModes.Keyboard),
            KeyDeviceType.Gamepad => modes.HasFlag(XYFocusNavigationModes.Gamepad),
            KeyDeviceType.Remote => modes.HasFlag(XYFocusNavigationModes.Remote),
            _ => throw new ArgumentOutOfRangeException(nameof(keyDeviceType), keyDeviceType, null)
        };
    }
}
