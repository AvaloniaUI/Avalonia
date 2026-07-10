using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives;

public class AdornerLayerTests : ScopedTestBase
{
    [Fact]
    public void Default_Focus_Adorner_Uses_Dark_Theme_Foreground()
    {
        using var application = UnitTestApplication.Start(TestServices.StyledWindow);

        var simpleTheme = new SimpleTheme();
        var adornerLayer = new AdornerLayer();
        var themeScope = new ThemeVariantScope
        {
            Child = adornerLayer,
            RequestedThemeVariant = ThemeVariant.Dark
        };
        var root = new TestRoot
        {
            Child = themeScope,
            Styles =
            {
                simpleTheme
            }
        };

        Assert.True(simpleTheme.TryGetResource(typeof(AdornerLayer), ThemeVariant.Dark, out var theme));
        adornerLayer.Theme = Assert.IsType<ControlTheme>(theme);
        root.ApplyStyling();
        themeScope.ApplyStyling();
        adornerLayer.ApplyStyling();

        var focusAdorner = Assert.IsType<Rectangle>(adornerLayer.DefaultFocusAdorner?.Build());
        adornerLayer.Children.Add(focusAdorner);

        var stroke = Assert.IsAssignableFrom<ISolidColorBrush>(focusAdorner.Stroke);
        Assert.Equal(Color.Parse("#FFDEDEDE"), stroke.Color);
    }
}
