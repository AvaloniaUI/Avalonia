using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Themes.Fluent2.UnitTests;

/// <summary>
/// Fluent2Theme.Palettes must keep v1 semantics: a user-supplied Accent flows
/// through SystemAccentColor and its shades into the WinUI accent token brushes
/// and the per-control accent keys.
/// </summary>
public class PaletteTests
{
    [AvaloniaFact]
    public void Palette_accent_flows_into_accent_tokens_and_button_keys()
    {
        var theme = new Fluent2Theme();
        theme.Palettes[ThemeVariant.Dark] = new ColorPaletteResources { Accent = Colors.Crimson };

        var app = Application.Current!;
        app.Styles.Add(theme);
        try
        {
            Assert.True(app.TryGetResource("SystemAccentColor", ThemeVariant.Dark, out var accent));
            Assert.Equal(Colors.Crimson, Assert.IsType<Color>(accent));

            // The dark-variant default accent fill is SystemAccentColorLight2 —
            // whatever shade the palette computed, the brush must match it.
            Assert.True(app.TryGetResource("SystemAccentColorLight2", ThemeVariant.Dark, out var light2));
            var light2Color = Assert.IsType<Color>(light2);
            Assert.NotEqual(Colors.Crimson, light2Color);

            Assert.True(app.TryGetResource("AccentFillColorDefaultBrush", ThemeVariant.Dark, out var fill));
            Assert.Equal(light2Color, Assert.IsAssignableFrom<ISolidColorBrush>(fill).Color);

            Assert.True(app.TryGetResource("AccentButtonBackground", ThemeVariant.Dark, out var buttonFill));
            Assert.Equal(light2Color, Assert.IsAssignableFrom<ISolidColorBrush>(buttonFill).Color);
        }
        finally
        {
            app.Styles.Remove(theme);
        }
    }

    [AvaloniaFact]
    public void Default_accent_uses_winui_static_shades()
    {
        var theme = new Fluent2Theme();
        var app = Application.Current!;
        app.Styles.Add(theme);
        try
        {
            Assert.True(app.TryGetResource("SystemAccentColor", ThemeVariant.Light, out var accent));
            Assert.Equal(Color.FromRgb(0, 120, 215), Assert.IsType<Color>(accent));

            // WinUI's static shade values (SystemResources.xaml), not HSL-computed ones.
            Assert.True(app.TryGetResource("SystemAccentColorLight2", ThemeVariant.Light, out var light2));
            Assert.Equal(Color.Parse("#FF76B9ED"), Assert.IsType<Color>(light2));
            Assert.True(app.TryGetResource("SystemAccentColorDark1", ThemeVariant.Light, out var dark1));
            Assert.Equal(Color.Parse("#FF005A9E"), Assert.IsType<Color>(dark1));
        }
        finally
        {
            app.Styles.Remove(theme);
        }
    }

    [AvaloniaFact]
    public void Compact_density_switches_metric_resources()
    {
        var theme = new Fluent2Theme();
        var app = Application.Current!;
        app.Styles.Add(theme);
        try
        {
            Assert.True(app.TryGetResource("TextControlThemeMinHeight", ThemeVariant.Light, out var normal));
            theme.DensityStyle = DensityStyle.Compact;
            Assert.True(app.TryGetResource("TextControlThemeMinHeight", ThemeVariant.Light, out var compact));
            Assert.True((double)compact! < (double)normal!,
                $"Compact min height {compact} should be smaller than normal {normal}");
        }
        finally
        {
            app.Styles.Remove(theme);
        }
    }
}
