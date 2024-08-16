using System;
using Avalonia.VisualTree;

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

    internal static InputElement? FindXYSearchRoot(this InputElement visual, KeyDeviceType? keyDeviceType)
    {
        InputElement candidate = visual;
        InputElement? candidateParent = visual.FindAncestorOfType<InputElement>();

        while (candidateParent is not null && candidateParent.IsAllowedXYNavigationMode(keyDeviceType))
        {
            candidate = candidateParent;
            candidateParent = candidate.FindAncestorOfType<InputElement>();
        }

        return candidate;
    }
}
