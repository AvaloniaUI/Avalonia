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

    [Fact]
    public void Host_ControlTheme_Style_For_PathIcon_Inside_Templated_Flyout_Should_Apply_On_First_Open()
    {
        using (CreateServicesWithoutTheme())
        {
            var expected = Brushes.Red;
            var icon = new PathIcon();
            var flyout = new Flyout { Content = icon };
            var host = new ContentControl
            {
                Template = new FuncControlTemplate<ContentControl>((_, _) =>
                    new DropDownButton { Flyout = flyout }),
                Theme = new ControlTheme(typeof(ContentControl))
                {
                    Children =
                    {
                        new Style(x => x.Nesting().Template().OfType<PathIcon>())
                        {
                            Setters = { new Setter(PathIcon.ForegroundProperty, expected) }
                        }
                    }
                }
            };
            var window = new Window { Content = host };

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
    
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Child_Selector_For_PathIcon_Inside_Templated_Flyout_Should_Apply_On_First_Open(bool useTheme)
    {
        using (CreateServicesWithoutTheme())
        {
            var icon = new PathIcon();
            var border = new Border { Child = icon };
            var flyout = new Flyout { Content = border };
            var host = new ContentControl
            {
                Template = new FuncControlTemplate<ContentControl>((_, _) =>
                    new DropDownButton { Flyout = flyout }),
            };
            var style = new Style(x => (useTheme ? x.Nesting() : x.OfType<ContentControl>())
                .Template().OfType<Border>().Child().OfType<PathIcon>())
            {
                Setters = { new Setter(PathIcon.ForegroundProperty, Brushes.Red) },
            };
            var window = new Window { Content = host };

            if (useTheme)
                host.Theme = new ControlTheme(typeof(ContentControl)) { Children = { style } };
            else
                window.Styles.Add(style);

            window.Show();
            host.ApplyTemplate();

            var dropDown = Assert.Single(host.GetVisualDescendants().OfType<DropDownButton>());
            flyout.ShowAt(dropDown);

            if (flyout.Popup.Host is OverlayPopupHost overlayHost)
            {
                overlayHost.Measure(Size.Infinity);
                overlayHost.Arrange(new Rect(overlayHost.DesiredSize));
            }

            Assert.Same(host, border.TemplatedParent);
            Assert.Same(host, icon.TemplatedParent);
            Assert.Equal(Brushes.Red, icon.Foreground);
        }
    }

    private IDisposable CreateServicesWithoutTheme()
    {
        return UnitTestApplication.Start(TestServices.StyledWindow.With(
            windowingPlatform: new MockWindowingPlatform(null,
                x => MockWindowingPlatform.CreatePopupMock(x).Object)));
    }
}
