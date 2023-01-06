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
/// Information about current system color values, including information about dark mode and accent colors.
/// </summary>
/// <param name="ThemeVariant">System theme variant or mode.</param>
/// <param name="AccentColor1">Primary system accent color.</param>
/// <param name="AccentColor2">Secondary system accent color. On some platforms can return the same value as AccentColor1.</param>
/// <param name="AccentColor3">Tertiary system accent color. On some platforms can return the same value as AccentColor1.</param>
public record struct PlatformColorValues(
    PlatformThemeVariant ThemeVariant,
    Color AccentColor1,
    Color AccentColor2,
    Color AccentColor3)
{
    public PlatformColorValues(
        PlatformThemeVariant ThemeVariant,
        Color AccentColor1)
        : this(ThemeVariant, AccentColor1, AccentColor1, AccentColor1)
    {
            
    }
        
    public PlatformColorValues(PlatformThemeVariant ThemeVariant)
        : this(ThemeVariant, DefaultAccent)
    {

    }
        
    private static Color DefaultAccent => new(255, 0, 120, 215);
}
