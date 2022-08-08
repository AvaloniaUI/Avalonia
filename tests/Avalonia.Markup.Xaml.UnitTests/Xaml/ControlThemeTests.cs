using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class ControlThemeTests : XamlTestBase
    {
        [Fact]
        public void ControlTheme_Can_Be_Inline()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <u:TestTemplatedControl>
        <u:TestTemplatedControl.Theme>
            {ControlThemeXaml(false)}
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.NotNull(button.Template);

                var child = Assert.Single(button.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);
            }
        }

        [Fact]
        public void ControlTheme_Can_Be_StaticResource()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <Window.Resources>
        {ControlThemeXaml(true)}
    </Window.Resources>

    <u:TestTemplatedControl Theme='{{StaticResource MyTheme}}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.NotNull(button.Template);

                var child = Assert.Single(button.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);
            }
        }

        [Fact]
        public void ControlTheme_Can_Be_Set_In_Style()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <Window.Resources>
        {ControlThemeXaml(true)}
    </Window.Resources>

    <Window.Styles>
        <Style Selector='u|TestTemplatedControl'>
            <Setter Property='Theme' Value='{{StaticResource MyTheme}}'/>
        </Style>
    </Window.Styles>

    <u:TestTemplatedControl/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.NotNull(button.Template);

                var child = Assert.Single(button.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);
            }
        }

        private static string ControlThemeXaml(bool withKey)
        {
            var key = withKey ? "x:Key='MyTheme' " : string.Empty;

            return $@"
<ControlTheme {key}TargetType='u:TestTemplatedControl'>
    <Setter Property='Template'>
        <ControlTemplate>
            <Border/>
        </ControlTemplate>
    </Setter>
    <Style Selector='^ /template/ Border'>
        <Setter Property='Background' Value='Red'/>
    </Style>
</ControlTheme>";
        }
    }
}
