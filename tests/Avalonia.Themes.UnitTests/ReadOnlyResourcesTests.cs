using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent2;
using Avalonia.Themes.UnitTests.Utilities;
using Xunit;

namespace Avalonia.Themes.UnitTests;

[Trait("Category", "Fluent2Theme")]
public class Fluent2ReadOnlyResourcesTests() : ReadOnlyResourcesTests(typeof(Fluent2Theme))
{
    [Fact]
    public void DensityStyle_Can_Be_Changed()
    {
        var theme = (Fluent2Theme)CreateAttachedTheme();
        var resources = (IResourceNode)theme;

        Assert.True(resources.TryGetResource("TextControlThemeMinHeight", ThemeVariant.Light, out var normal));
        Assert.Equal(32.0, normal);

        theme.DensityStyle = DensityStyle.Compact;
        Assert.True(resources.TryGetResource("TextControlThemeMinHeight", ThemeVariant.Light, out var compact));
        Assert.Equal(24.0, compact);
    }

    [Fact]
    public void Palettes_Can_Be_Changed()
    {
        var theme = (Fluent2Theme)CreateAttachedTheme();

        theme.Palettes[ThemeVariant.Light] = new ColorPaletteResources { Accent = Colors.Red };

        Assert.True(((IResourceNode)theme).TryGetResource("SystemAccentColor", ThemeVariant.Light, out var accent));
        Assert.Equal(Colors.Red, accent);
    }
}

public abstract class ReadOnlyResourcesTests(Type typeEntryPoint) : ThemeTestBase(typeEntryPoint)
{
    [Fact]
    public void Theme_Resources_Are_ReadOnly()
    {
        var theme = CreateAttachedTheme();

        var resources = Assert.IsType<ResourceDictionary>(theme.Resources);
        Assert.True(resources.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => resources.Add("Test", 1.0));
        Assert.Throws<NotSupportedException>(() => resources.MergedDictionaries.Add(new ResourceDictionary()));
        Assert.All(resources.ThemeDictionaries.Values, d => Assert.True(Assert.IsType<ResourceDictionary>(d).IsReadOnly));
        // Compiled ResourceInclude is replaced with a ResourceDictionary, other providers are not affected.
        Assert.All(resources.MergedDictionaries, d => Assert.True(d is not ResourceDictionary { IsReadOnly: false }));
        Assert.Contains(resources.MergedDictionaries, d => d is ResourceDictionary);
    }
}
