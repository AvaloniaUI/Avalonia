using System;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class DropDownButtonTests : ScopedTestBase
{
    [Fact]
    public void Window_Style_For_PathIcon_Inside_Templated_Flyout_Should_Apply_On_First_Open()
    {
        using (CreateServicesWithoutTheme())
        {
            var expected = Brushes.Red;
            var icon = new PathIcon();
            var flyout = new Flyout { Content = icon };
            var host = new ContentControl
            {
                Template = new FuncControlTemplate<ContentControl>((_, _) =>
                    new DropDownButton { Flyout = flyout })
            };
            var window = new Window { Content = host };

            window.Styles.Add(new Style(x => x.OfType<ContentControl>().Template().OfType<PathIcon>())
            {
                Setters = { new Setter(PathIcon.ForegroundProperty, expected) }
            });

            window.Show();
            window.ApplyStyling();
            window.ApplyTemplate();
            host.ApplyTemplate();

            var dropDown = Assert.IsType<DropDownButton>(host.GetVisualDescendants().OfType<DropDownButton>().Single());
            flyout.ShowAt(dropDown);

            if (flyout.Popup.Host is OverlayPopupHost overlayHost)
            {
                overlayHost.Measure(Size.Infinity);
                overlayHost.Arrange(new Rect(overlayHost.DesiredSize));
            }

            Assert.Equal(expected, icon.Foreground);
        }
    }
    
    private IDisposable CreateServicesWithoutTheme()
    {
        return UnitTestApplication.Start(TestServices.StyledWindow.With(
            windowingPlatform: new MockWindowingPlatform(null,
                x => MockWindowingPlatform.CreatePopupMock(x).Object)));
    }
}
