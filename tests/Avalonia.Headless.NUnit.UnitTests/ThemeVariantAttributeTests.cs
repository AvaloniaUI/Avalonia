using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.Headless.NUnit.UnitTests;

public class ThemeVariantValuesAttributeTests
{
    [AvaloniaTest, Timeout(10000)]
    public void ThemeVariantValuesTest([ThemeVariantValues] ThemeVariant theme)
    {
        Application.Current!.RequestedThemeVariant = theme;

        var window = new Window();
        window.Show();

        Assert.AreEqual(theme, window.ActualThemeVariant);
    }
}
