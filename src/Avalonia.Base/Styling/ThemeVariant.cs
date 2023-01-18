using System;
using System.ComponentModel;
using System.Text;
using Avalonia.Platform;

namespace Avalonia.Styling;

[TypeConverter(typeof(ThemeVariantTypeConverter))]
public sealed record ThemeVariant(object Key)
{ 
    public ThemeVariant(object key, ThemeVariant? inheritVariant)
        : this(key)
    {
        InheritVariant = inheritVariant;
    }

    public static ThemeVariant Default { get; } = new(nameof(Default));
    public static ThemeVariant Light { get; } = new(nameof(Light));
    public static ThemeVariant Dark { get; } = new(nameof(Dark));

    public ThemeVariant? InheritVariant { get; init; }

    public override string ToString()
    {
        return Key.ToString() ?? $"ThemeVariant {{ Key = {Key} }}";
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }

    public bool Equals(ThemeVariant? other)
    {
        return Key == other?.Key;
    }

    public static ThemeVariant FromPlatformThemeVariant(PlatformThemeVariant themeVariant)
    {
        return themeVariant switch
        {
            PlatformThemeVariant.Light => Light,
            PlatformThemeVariant.Dark => Dark,
            _ => throw new ArgumentOutOfRangeException(nameof(themeVariant), themeVariant, null)
        };
    }

    public PlatformThemeVariant? ToPlatformThemeVariant()
    {
        if (this == Light)
        {
            return PlatformThemeVariant.Light;
        }
        else if (this == Dark)
        {
            return PlatformThemeVariant.Dark;
        }
        else if (InheritVariant is { } inheritVariant)
        {
            return inheritVariant.ToPlatformThemeVariant();
        }

        return null;
    }
}
