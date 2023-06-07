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

    public PlatformColorValues()
    {
        AccentColor1 = DefaultAccent;
    }
}
