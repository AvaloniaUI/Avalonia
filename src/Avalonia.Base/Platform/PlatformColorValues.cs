using Avalonia.Media;

namespace Avalonia.Platform;

/// <summary>
/// System theme variant or mode.
/// </summary>
public enum PlatformThemeVariant
{
    Light,
    Dark
}

/// <summary>
/// System high contrast preference.
/// </summary>
public enum ColorContrastPreference
{
    NoPreference,
    High
}

/// <summary>
/// Information about current system color values, including information about dark mode and accent colors.
/// </summary>
public record PlatformColorValues
{
    private static Color DefaultAccent => new(255, 0, 120, 215);
    private Color _accentColor2, _accentColor3;

    /// <summary>
    /// System theme variant or mode.
    /// </summary>
    public PlatformThemeVariant ThemeVariant { get; init; }

    /// <summary>
    /// System high contrast preference.
    /// </summary>
    public ColorContrastPreference ContrastPreference { get; init; }
    
    /// <summary>
    /// Primary system accent color.
    /// </summary>
    public Color AccentColor1 { get; init; }

    /// <summary>
    /// Secondary system accent color. On some platforms can return the same value as <see cref="AccentColor1"/>.
    /// </summary>
    public Color AccentColor2
    {
        get => _accentColor2 != default ? _accentColor2 : AccentColor1;
        init => _accentColor2 = value;
    }

    /// <summary>
    /// Tertiary system accent color. On some platforms can return the same value as <see cref="AccentColor1"/>.
    /// </summary>
    public Color AccentColor3
    {
        get => _accentColor3 != default ? _accentColor3 : AccentColor1;
        init => _accentColor3 = value;
    }

    /// <summary>
    /// Dark shade 1 of <see cref="AccentColor1"/>, when the platform provides one (Windows does).
    /// Null when the platform only reports the base accent color; consumers are expected to compute a shade instead.
    /// </summary>
    public Color? AccentColorDark1 { get; init; }

    /// <summary>
    /// Dark shade 2 of <see cref="AccentColor1"/>, when the platform provides one. See <see cref="AccentColorDark1"/>.
    /// </summary>
    public Color? AccentColorDark2 { get; init; }

    /// <summary>
    /// Dark shade 3 of <see cref="AccentColor1"/>, when the platform provides one. See <see cref="AccentColorDark1"/>.
    /// </summary>
    public Color? AccentColorDark3 { get; init; }

    /// <summary>
    /// Light shade 1 of <see cref="AccentColor1"/>, when the platform provides one. See <see cref="AccentColorDark1"/>.
    /// </summary>
    public Color? AccentColorLight1 { get; init; }

    /// <summary>
    /// Light shade 2 of <see cref="AccentColor1"/>, when the platform provides one. See <see cref="AccentColorDark1"/>.
    /// </summary>
    public Color? AccentColorLight2 { get; init; }

    /// <summary>
    /// Light shade 3 of <see cref="AccentColor1"/>, when the platform provides one. See <see cref="AccentColorDark1"/>.
    /// </summary>
    public Color? AccentColorLight3 { get; init; }

    public PlatformColorValues()
    {
        AccentColor1 = DefaultAccent;
    }
}
