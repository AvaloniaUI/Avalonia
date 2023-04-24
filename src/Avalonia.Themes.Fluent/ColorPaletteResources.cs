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
public partial class ColorPaletteResources : AvaloniaObject, IResourceNode
{
    private readonly Dictionary<string, Color> _colors = new(StringComparer.InvariantCulture);

    public bool HasResources => _hasAccentColor || _colors.Count > 0;

    public bool TryGetResource(object key, ThemeVariant? theme, out object? value)
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
        }
    }
}
