using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Diagnostics;
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
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.NotNull(target.Template);

                var child = Assert.Single(target.GetVisualChildren());
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
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.NotNull(target.Template);

                var child = Assert.Single(target.GetVisualChildren());
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
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.NotNull(target.Template);

                var child = Assert.Single(target.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);
            }
        }

        [Fact]
        public void Literal_Value_In_Template_Is_Set_With_ControlTheme_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <u:TestTemplatedControl>
        <u:TestTemplatedControl.Theme>
            <ControlTheme TargetType='u:TestTemplatedControl'>
                <Setter Property='Template'>
                    <ControlTemplate>
                        <Border Background='Red'/>
                    </ControlTemplate>
                </Setter>
            </ControlTheme>
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.NotNull(target.Template);

                var child = Assert.Single(target.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);

                var diagnostic = border.GetDiagnostic(Border.BackgroundProperty);
                Assert.Equal(BindingPriority.ControlTheme, diagnostic.Priority);
            }
        }

        [Fact]
        public void StaticResource_In_Template_Is_Set_With_ControlTheme_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='red'>Red</SolidColorBrush>
    </Window.Resources>
    <u:TestTemplatedControl>
        <u:TestTemplatedControl.Theme>
            <ControlTheme TargetType='u:TestTemplatedControl'>
                <Setter Property='Template'>
                    <ControlTemplate>
                        <Border Background='{{StaticResource red}}'/>
                    </ControlTemplate>
                </Setter>
            </ControlTheme>
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.NotNull(target.Template);

                var child = Assert.Single(target.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);

                var diagnostic = border.GetDiagnostic(Border.BackgroundProperty);
                Assert.Equal(BindingPriority.ControlTheme, diagnostic.Priority);
            }
        }

        [Fact]
        public void DynamicResource_In_Template_Is_Set_With_ControlTheme_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='red'>Red</SolidColorBrush>
    </Window.Resources>
    <u:TestTemplatedControl>
        <u:TestTemplatedControl.Theme>
            <ControlTheme TargetType='u:TestTemplatedControl'>
                <Setter Property='Template'>
                    <ControlTemplate>
                        <Border Background='{{DynamicResource red}}'/>
                    </ControlTemplate>
                </Setter>
            </ControlTheme>
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.NotNull(target.Template);

                var child = Assert.Single(target.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);

                var diagnostic = border.GetDiagnostic(Border.BackgroundProperty);
                Assert.Equal(BindingPriority.ControlTheme, diagnostic.Priority);
            }
        }

        [Fact]
        public void TemplateBinding_In_Template_Is_Set_With_ControlTheme_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <u:TestTemplatedControl Background='Red'>
        <u:TestTemplatedControl.Theme>
            <ControlTheme TargetType='u:TestTemplatedControl'>
                <Setter Property='Template'>
                    <ControlTemplate>
                        <Border Background='{{TemplateBinding Background}}'/>
                    </ControlTemplate>
                </Setter>
            </ControlTheme>
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.NotNull(target.Template);

                var child = Assert.Single(target.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);

                var diagnostic = border.GetDiagnostic(Border.BackgroundProperty);
                Assert.Equal(BindingPriority.ControlTheme, diagnostic.Priority);
            }
        }

        [Fact]
        public void Setter_Value_Is_Set_With_ControlTheme_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <u:TestTemplatedControl>
        <u:TestTemplatedControl.Theme>
            <ControlTheme TargetType='u:TestTemplatedControl'>
                <Setter Property='Background' Value='Red'/>
            </ControlTheme>
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(Brushes.Red, target.Background);

                var diagnostic = target.GetDiagnostic(Border.BackgroundProperty);
                Assert.Equal(BindingPriority.ControlTheme, diagnostic.Priority);
            }
        }

        [Fact]
        public void Setter_StaticResource_Is_Set_With_ControlTheme_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='red'>Red</SolidColorBrush>
    </Window.Resources>
    <u:TestTemplatedControl>
        <u:TestTemplatedControl.Theme>
            <ControlTheme TargetType='u:TestTemplatedControl'>
                <Setter Property='Background' Value='{{StaticResource red}}'/>
            </ControlTheme>
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(Brushes.Red, target.Background);

                var diagnostic = target.GetDiagnostic(Border.BackgroundProperty);
                Assert.Equal(BindingPriority.ControlTheme, diagnostic.Priority);
            }
        }

        [Fact]
        public void Setter_DynamicResource_Is_Set_With_ControlTheme_Priority()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='red'>Red</SolidColorBrush>
    </Window.Resources>
    <u:TestTemplatedControl>
        <u:TestTemplatedControl.Theme>
            <ControlTheme TargetType='u:TestTemplatedControl'>
                <Setter Property='Background' Value='{{DynamicResource red}}'/>
            </ControlTheme>
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(Brushes.Red, target.Background);

                var diagnostic = target.GetDiagnostic(Border.BackgroundProperty);
                Assert.Equal(BindingPriority.ControlTheme, diagnostic.Priority);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Nested_Style_Setter_Value_Is_Set_With_Correct_Priority(bool isTrigger)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var trigger = isTrigger ? ".foo" : string.Empty;

                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <u:TestTemplatedControl Classes='foo'>
        <u:TestTemplatedControl.Theme>
            <ControlTheme TargetType='u:TestTemplatedControl'>
                <Setter Property='Template'>
                    <ControlTemplate>
                        <Border/>
                    </ControlTemplate>
                </Setter>
                <Style Selector='^{trigger} /template/ Border'>
                    <Setter Property='Background' Value='Red'/>
                </Style>
            </ControlTheme>
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                var child = Assert.Single(target.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);

                var diagnostic = border.GetDiagnostic(Border.BackgroundProperty);
                var expected = isTrigger ? BindingPriority.ControlThemeTrigger : BindingPriority.ControlTheme;
                Assert.Equal(expected, diagnostic.Priority);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Nested_Style_Setter_StaticResource_Is_Set_With_Correct_Priority(bool isTrigger)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var trigger = isTrigger ? ".foo" : string.Empty;

                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='red'>Red</SolidColorBrush>
    </Window.Resources>
    <u:TestTemplatedControl Classes='foo'>
        <u:TestTemplatedControl.Theme>
            <ControlTheme TargetType='u:TestTemplatedControl'>
                <Setter Property='Template'>
                    <ControlTemplate>
                        <Border/>
                    </ControlTemplate>
                </Setter>
                <Style Selector='^{trigger} /template/ Border'>
                    <Setter Property='Background' Value='{{StaticResource red}}'/>
                </Style>
            </ControlTheme>
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                var child = Assert.Single(target.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);

                var diagnostic = border.GetDiagnostic(Border.BackgroundProperty);
                var expected = isTrigger ? BindingPriority.ControlThemeTrigger : BindingPriority.ControlTheme;
                Assert.Equal(expected, diagnostic.Priority);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Nested_Style_Setter_DynamicResource_Is_Set_With_Correct_Priority(bool isTrigger)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var trigger = isTrigger ? ".foo" : string.Empty;

                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='red'>Red</SolidColorBrush>
    </Window.Resources>
    <u:TestTemplatedControl Classes='foo'>
        <u:TestTemplatedControl.Theme>
            <ControlTheme TargetType='u:TestTemplatedControl'>
                <Setter Property='Template'>
                    <ControlTemplate>
                        <Border/>
                    </ControlTemplate>
                </Setter>
                <Style Selector='^{trigger} /template/ Border'>
                    <Setter Property='Background' Value='{{DynamicResource red}}'/>
                </Style>
            </ControlTheme>
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                var child = Assert.Single(target.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);

                var diagnostic = border.GetDiagnostic(Border.BackgroundProperty);
                var expected = isTrigger ? BindingPriority.ControlThemeTrigger : BindingPriority.ControlTheme;
                Assert.Equal(expected, diagnostic.Priority);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Nested_Style_Setter_TemplateBinding_Is_Set_With_Correct_Priority(bool isTrigger)
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var trigger = isTrigger ? ".foo" : string.Empty;

                var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='red'>Red</SolidColorBrush>
    </Window.Resources>
    <u:TestTemplatedControl Classes='foo' Background='Red'>
        <u:TestTemplatedControl.Theme>
            <ControlTheme TargetType='u:TestTemplatedControl'>
                <Setter Property='Template'>
                    <ControlTemplate>
                        <Border/>
                    </ControlTemplate>
                </Setter>
                <Style Selector='^{trigger} /template/ Border'>
                    <Setter Property='Background' Value='{{TemplateBinding Background}}'/>
                </Style>
            </ControlTheme>
        </u:TestTemplatedControl.Theme>
    </u:TestTemplatedControl>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target = Assert.IsType<TestTemplatedControl>(window.Content);

                window.LayoutManager.ExecuteInitialLayoutPass();

                var child = Assert.Single(target.GetVisualChildren());
                var border = Assert.IsType<Border>(child);

                Assert.Equal(Brushes.Red, border.Background);

                var diagnostic = border.GetDiagnostic(Border.BackgroundProperty);
                var expected = isTrigger ? BindingPriority.ControlThemeTrigger : BindingPriority.ControlTheme;
                Assert.Equal(expected, diagnostic.Priority);
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
