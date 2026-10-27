using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent2.Accents;

namespace Avalonia.Themes.Fluent2;

/// <summary>
/// Represents a specialized resource dictionary that contains color resources used by Fluent2Theme elements.
/// </summary>
/// <remarks>
/// This class can only be used in <see cref="Fluent2Theme.Palettes"/>.
/// </remarks>
public partial class ColorPaletteResources : ResourceProvider
{
    private readonly Dictionary<string, Color> _colors = new(StringComparer.InvariantCulture);

    private readonly record struct TokenDerivation(string SourceKey, byte? LightAlpha, byte? DarkAlpha);

    // Legacy palette colors also drive the nearest Fluent 2 (WinUI 3) tokens so that
    // v1-era palette customization keeps affecting the new visuals. The token's own
    // per-variant alpha is applied over the user's RGB; a null alpha keeps the user's.
    // This is a documented heuristic — for exact control override the token keys directly.
    private static readonly Dictionary<string, TokenDerivation[]> s_tokenDerivations = new(StringComparer.InvariantCulture)
    {
        ["TextFillColorPrimary"] = new[] { new TokenDerivation("SystemBaseHighColor", 0xE4, 0xFF) },
        ["TextFillColorSecondary"] = new[] { new TokenDerivation("SystemBaseMediumHighColor", 0x9E, 0xC5) },
        ["TextFillColorTertiary"] = new[] { new TokenDerivation("SystemBaseMediumColor", 0x72, 0x87) },
        ["ControlFillColorDefault"] = new[] { new TokenDerivation("SystemBaseLowColor", 0xB3, 0x0F) },
        ["ControlFillColorSecondary"] = new[] { new TokenDerivation("SystemBaseLowColor", 0x80, 0x15) },
        ["ControlFillColorTertiary"] = new[] { new TokenDerivation("SystemBaseLowColor", 0x4D, 0x08) },
        ["SolidBackgroundFillColorBase"] = new[] { new TokenDerivation("SystemRegionColor", null, null) },
        ["SolidBackgroundFillColorSecondary"] = new[] { new TokenDerivation("SystemRegionColor", null, null) },
        ["SolidBackgroundFillColorTertiary"] = new[] { new TokenDerivation("SystemRegionColor", null, null) },
        ["SolidBackgroundFillColorQuarternary"] = new[]
        {
            new TokenDerivation("SystemChromeMediumColor", null, null),
            new TokenDerivation("SystemRegionColor", null, null),
        },
        ["SystemFillColorCritical"] = new[] { new TokenDerivation("SystemErrorTextColor", null, null) },
    };

    public override bool HasResources => _hasAccentColor || _colors.Count > 0;

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is string strKey)
        {
            if (strKey.Equals(SystemAccentColors.AccentKey, StringComparison.InvariantCulture))
            {
                value = _accentColor;
                return _hasAccentColor;
            }

            if (strKey.Equals(SystemAccentColors.AccentDark1Key, StringComparison.InvariantCulture))
            {
                value = _accentColorDark1;
                return _hasAccentColor;
            }

            if (strKey.Equals(SystemAccentColors.AccentDark2Key, StringComparison.InvariantCulture))
            {
                value = _accentColorDark2;
                return _hasAccentColor;
            }

            if (strKey.Equals(SystemAccentColors.AccentDark3Key, StringComparison.InvariantCulture))
            {
                value = _accentColorDark3;
                return _hasAccentColor;
            }

            if (strKey.Equals(SystemAccentColors.AccentLight1Key, StringComparison.InvariantCulture))
            {
                value = _accentColorLight1;
                return _hasAccentColor;
            }

            if (strKey.Equals(SystemAccentColors.AccentLight2Key, StringComparison.InvariantCulture))
            {
                value = _accentColorLight2;
                return _hasAccentColor;
            }

            if (strKey.Equals(SystemAccentColors.AccentLight3Key, StringComparison.InvariantCulture))
            {
                value = _accentColorLight3;
                return _hasAccentColor;
            }

            if (_colors.TryGetValue(strKey, out var color))
            {
                value = color;
                return true;
            }

            if (TryGetDerivedTokenColor(strKey, theme, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    private bool TryGetDerivedTokenColor(string key, ThemeVariant? theme, out object? value)
    {
        if (s_tokenDerivations.TryGetValue(key, out var derivations))
        {
            foreach (var derivation in derivations)
            {
                if (_colors.TryGetValue(derivation.SourceKey, out var source))
                {
                    var alpha = theme == ThemeVariant.Dark ? derivation.DarkAlpha : derivation.LightAlpha;
                    value = alpha is { } a ? new Color(a, source.R, source.G, source.B) : source;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    private Color GetColor(string key)
    {
        if (_colors.TryGetValue(key, out var color))
        {
            return color;
        }

        return default;
    }

    private void SetColor(string key, Color value)
    {
        if (value == default)
        {
            _colors.Remove(key);
        }
        else
        {
            _colors[key] = value;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AccentProperty)
        {
            _hasAccentColor = _accentColor != default;

            if (_hasAccentColor)
            {
                (_accentColorDark1, _accentColorDark2, _accentColorDark3,
                        _accentColorLight1, _accentColorLight2, _accentColorLight3) =
                    SystemAccentColors.CalculateAccentShades(_accentColor);
            }
            RaiseResourcesChanged();
        }
    }
}
