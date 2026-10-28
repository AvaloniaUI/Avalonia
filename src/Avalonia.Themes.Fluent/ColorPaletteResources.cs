using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent.Accents;

namespace Avalonia.Themes.Fluent;

/// <summary>
/// Represents a specialized resource dictionary that contains color resources used by FluentTheme elements.
/// </summary>
/// <remarks>
/// This class can only be used in <see cref="FluentTheme.Palettes"/>.
/// </remarks>
public partial class ColorPaletteResources : ResourceProvider
{
    private readonly Dictionary<string, Color> _colors = new(StringComparer.Ordinal);

    public override bool HasResources => _hasAccentColor || _colors.Count > 0;

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is string strKey)
        {
            switch (strKey)
            {
                case SystemAccentColors.AccentKey:
                    value = _accentColor;
                    return _hasAccentColor;
                case SystemAccentColors.AccentDark1Key:
                    value = _accentColorDark1;
                    return _hasAccentColor;
                case SystemAccentColors.AccentDark2Key:
                    value = _accentColorDark2;
                    return _hasAccentColor;
                case SystemAccentColors.AccentDark3Key:
                    value = _accentColorDark3;
                    return _hasAccentColor;
                case SystemAccentColors.AccentLight1Key:
                    value = _accentColorLight1;
                    return _hasAccentColor;
                case SystemAccentColors.AccentLight2Key:
                    value = _accentColorLight2;
                    return _hasAccentColor;
                case SystemAccentColors.AccentLight3Key:
                    value = _accentColorLight3;
                    return _hasAccentColor;
            }

            if (_colors.TryGetValue(strKey, out var color))
            {
                value = color;
                return true;
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
