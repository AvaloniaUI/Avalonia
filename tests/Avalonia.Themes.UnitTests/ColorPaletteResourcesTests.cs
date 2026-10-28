using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.UnitTests.Utilities;
using Xunit;

namespace Avalonia.Themes.UnitTests;

[Trait("Category", "FluentTheme")]
public class FluentColorPaletteResourcesTests() : ThemeTestBase(typeof(FluentTheme))
{
    [Theory]
    [InlineData("SystemAccentColor")]
    [InlineData("SystemAccentColorDark1")]
    [InlineData("SystemAccentColorDark2")]
    [InlineData("SystemAccentColorDark3")]
    [InlineData("SystemAccentColorLight1")]
    [InlineData("SystemAccentColorLight2")]
    [InlineData("SystemAccentColorLight3")]
    public void System_Accent_Colors_Are_Resolved(string key)
    {
        var theme = (IResourceNode)CreateAttachedTheme();

        Assert.True(theme.TryGetResource(key, ThemeVariant.Light, out var value));
        Assert.IsType<Color>(value);
    }

    [Fact]
    public void Palette_Accent_Overrides_System_Accent_Colors()
    {
        var theme = (FluentTheme)CreateAttachedTheme();

        theme.Palettes[ThemeVariant.Light] = new ColorPaletteResources { Accent = Colors.Red };

        Assert.True(((IResourceNode)theme).TryGetResource("SystemAccentColor", ThemeVariant.Light, out var accent));
        Assert.Equal(Colors.Red, accent);
        Assert.True(((IResourceNode)theme).TryGetResource("SystemAccentColorDark1", ThemeVariant.Light, out var dark1));
        Assert.NotEqual(Colors.Red, dark1);
    }

    [Fact]
    public void Palette_Color_Overrides_Theme_Color()
    {
        var theme = (FluentTheme)CreateAttachedTheme();

        theme.Palettes[ThemeVariant.Light] = new ColorPaletteResources { RegionColor = Colors.Red };

        Assert.True(((IResourceNode)theme).TryGetResource("SystemRegionColor", ThemeVariant.Light, out var region));
        Assert.Equal(Colors.Red, region);
    }

    [Fact]
    public void Palette_Keys_Are_Case_Sensitive()
    {
        var palette = new ColorPaletteResources { Accent = Colors.Red, RegionColor = Colors.Red };

        Assert.True(palette.TryGetResource("SystemAccentColor", ThemeVariant.Light, out _));
        Assert.True(palette.TryGetResource("SystemRegionColor", ThemeVariant.Light, out _));
        Assert.False(palette.TryGetResource("systemaccentcolor", ThemeVariant.Light, out _));
        Assert.False(palette.TryGetResource("systemregioncolor", ThemeVariant.Light, out _));
    }
}
