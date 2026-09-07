using System;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class ThemeVariantTests : ScopedTestBase
{
    [Fact]
    public void Application_ActualThemeVariant_Falls_Back_To_Light_Without_Platform_Settings()
    {
        var application = new Application();
        application.InitializeThemeVariant();

        Assert.Equal(ThemeVariant.Light, application.ActualThemeVariant);
    }

    [Theory]
    [InlineData(PlatformThemeVariant.Light)]
    [InlineData(PlatformThemeVariant.Dark)]
    public void Application_ActualThemeVariant_Is_Initialized_From_Platform_Settings(
        PlatformThemeVariant platformThemeVariant)
    {
        using var app = StartApplication(new TestPlatformSettings(platformThemeVariant));

        Assert.Equal((ThemeVariant)platformThemeVariant, Application.Current!.ActualThemeVariant);
    }

    [Fact]
    public void Application_ActualThemeVariant_Follows_Platform_Settings_Changes()
    {
        var platformSettings = new TestPlatformSettings(PlatformThemeVariant.Light);
        using var app = StartApplication(platformSettings);
        var application = Application.Current!;

        var raised = 0;
        application.ActualThemeVariantChanged += (_, _) => ++raised;

        platformSettings.ThemeVariant = PlatformThemeVariant.Dark;

        Assert.Equal(ThemeVariant.Dark, application.ActualThemeVariant);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Application_RequestedThemeVariant_Overrides_Platform_Settings()
    {
        var platformSettings = new TestPlatformSettings(PlatformThemeVariant.Light);
        using var app = StartApplication(platformSettings);
        var application = Application.Current!;

        application.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(ThemeVariant.Dark, application.ActualThemeVariant);

        platformSettings.ThemeVariant = PlatformThemeVariant.Light;
        Assert.Equal(ThemeVariant.Dark, application.ActualThemeVariant);
    }

    [Fact]
    public void Application_ActualThemeVariant_Reverts_To_Platform_Settings_When_Requesting_Default()
    {
        var platformSettings = new TestPlatformSettings(PlatformThemeVariant.Dark);
        using var app = StartApplication(platformSettings);
        var application = Application.Current!;

        application.RequestedThemeVariant = ThemeVariant.Light;
        Assert.Equal(ThemeVariant.Light, application.ActualThemeVariant);

        application.RequestedThemeVariant = ThemeVariant.Default;
        Assert.Equal(ThemeVariant.Dark, application.ActualThemeVariant);
    }

    [Fact]
    public void Application_ActualThemeVariant_Reverts_To_Platform_Settings_When_Requesting_Null()
    {
        var platformSettings = new TestPlatformSettings(PlatformThemeVariant.Dark);
        using var app = StartApplication(platformSettings);
        var application = Application.Current!;

        application.RequestedThemeVariant = ThemeVariant.Light;
        Assert.Equal(ThemeVariant.Light, application.ActualThemeVariant);

        application.RequestedThemeVariant = null;
        Assert.Equal(ThemeVariant.Dark, application.ActualThemeVariant);
    }

    [Fact]
    public void Application_Custom_ThemeVariant_Is_Used_As_Is()
    {
        var custom = new ThemeVariant("Custom", ThemeVariant.Dark);
        using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Light));
        var application = Application.Current!;

        application.RequestedThemeVariant = custom;

        Assert.Equal(custom, application.ActualThemeVariant);
    }

    [Fact]
    public void TopLevel_ActualThemeVariant_Is_Initialized_From_Application()
    {
        using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Light));

        BindApplicationAsThemeVariantHost();
        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;

        var window = new Window();

        Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
    }

    [Fact]
    public void TopLevel_ActualThemeVariant_Is_Initialized_From_Platform_Settings_Without_ThemeVariantHost()
    {
        using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Dark));

        var window = new Window();

        Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
    }

    [Fact]
    public void TopLevel_ActualThemeVariant_Follows_Application()
    {
        var platformSettings = new TestPlatformSettings(PlatformThemeVariant.Light);
        using var app = StartApplication(platformSettings);
        var application = Application.Current!;

        BindApplicationAsThemeVariantHost();

        var window = new Window();
        Assert.Equal(ThemeVariant.Light, window.ActualThemeVariant);

        application.RequestedThemeVariant = ThemeVariant.Dark;
        Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);

        application.RequestedThemeVariant = ThemeVariant.Default;
        Assert.Equal(ThemeVariant.Light, window.ActualThemeVariant);

        platformSettings.ThemeVariant = PlatformThemeVariant.Dark;
        Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
    }

    [Fact]
    public void TopLevel_RequestedThemeVariant_Overrides_Application_And_Is_Sent_To_Platform_Impl()
    {
        using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Dark));

        BindApplicationAsThemeVariantHost();

        var window = new Window { RequestedThemeVariant = ThemeVariant.Light };

        Assert.Equal(ThemeVariant.Dark, Application.Current!.ActualThemeVariant);
        Assert.Equal(ThemeVariant.Light, window.ActualThemeVariant);
    }

    [Fact]
    public void TopLevel_ActualThemeVariant_Is_Inherited_By_Children()
    {
        using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Dark));

        BindApplicationAsThemeVariantHost();

        var child = new Border();
        var scope = new ThemeVariantScope { Child = child };
        var window = new Window { Content = scope };
        window.Show();

        Assert.Equal(ThemeVariant.Dark, scope.ActualThemeVariant);
        Assert.Equal(ThemeVariant.Dark, child.ActualThemeVariant);

        scope.RequestedThemeVariant = ThemeVariant.Light;

        Assert.Equal(ThemeVariant.Dark, window.ActualThemeVariant);
        Assert.Equal(ThemeVariant.Light, scope.ActualThemeVariant);
        Assert.Equal(ThemeVariant.Light, child.ActualThemeVariant);
    }

    [Fact]
    public void ThemeVariantScope_Requesting_Default_Reinherits_From_Parent()
    {
        using var app = StartApplication(new TestPlatformSettings(PlatformThemeVariant.Light));

        BindApplicationAsThemeVariantHost();
        Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;

        var child = new Border();
        var scope = new ThemeVariantScope { Child = child };
        var window = new Window { Content = scope };
        window.Show();

        scope.RequestedThemeVariant = ThemeVariant.Light;
        Assert.Equal(ThemeVariant.Light, scope.ActualThemeVariant);
        Assert.Equal(ThemeVariant.Light, child.ActualThemeVariant);

        scope.RequestedThemeVariant = ThemeVariant.Default;
        Assert.Equal(ThemeVariant.Dark, scope.ActualThemeVariant);
        Assert.Equal(ThemeVariant.Dark, child.ActualThemeVariant);
    }

    private static IDisposable StartApplication(TestPlatformSettings platformSettings)
        => UnitTestApplication.Start(TestServices.StyledWindow.With(
            platformSettings: platformSettings,
            windowingPlatform: new MockWindowingPlatform()));

    private static void BindApplicationAsThemeVariantHost()
        => AvaloniaLocator.CurrentMutable.Bind<IThemeVariantHost>().ToConstant(Application.Current!);
}
