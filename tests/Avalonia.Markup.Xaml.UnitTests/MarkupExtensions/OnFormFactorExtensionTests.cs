using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;

public class OnFormFactorExtensionTests : XamlTestBase
{
    [Fact]
    public void Should_Resolve_Default_Value()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(false, false));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Text='{OnFormFactor Default=""Hello World""}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var textBlock = (TextBlock)userControl.Content!;

            Assert.Equal("Hello World", textBlock.Text);
        }
    }

    [Fact]
    public void Should_Resolve_Default_Value_From_Ctor()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(false, false));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Text='{OnFormFactor ""Hello World"", Mobile=""Im Mobile""}' Margin='10' />
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var textBlock = (TextBlock)userControl.Content!;

            Assert.Equal("Hello World", textBlock.Text);
        }
    }

    [Theory]
    [InlineData(false, true, "Im Mobile")]
    [InlineData(true, false, "Im Desktop")]
    [InlineData(false, false, "Default value")]
    public void Should_Resolve_Expected_Value_Per_Platform(bool isDesktop, bool isMobile, string expectedResult)
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(isDesktop, isMobile));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Text='{OnFormFactor ""Default value"",
                                 Mobile=""Im Mobile"", Desktop=""Im Desktop""}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var textBlock = (TextBlock)userControl.Content!;

            Assert.Equal(expectedResult, textBlock.Text);
        }
    }

    [Fact]
    public void Should_Convert_Bcl_Type()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(true, false));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Height='{OnFormFactor Default=50.1}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = (Border)userControl.Content!;

            Assert.Equal(50.1, border.Height);
        }
    }

    [Fact]
    public void Should_Convert_Avalonia_Type()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(true, false));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Padding='{OnFormFactor Default=""10, 8, 10, 8""}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = (Border)userControl.Content!;

            Assert.Equal(new Thickness(10, 8, 10, 8), border.Padding);
        }
    }

    [Fact]
    public void Should_Respect_Custom_TypeArgument()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(true, false));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock DataContext='{OnFormFactor Default=""10, 10, 10, 10"", x:TypeArguments=Thickness}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var textBlock = (TextBlock)userControl.Content!;

            Assert.Equal(new Thickness(10, 10, 10, 10), textBlock.DataContext);
        }
    }

    [Fact]
    public void Should_Allow_Nester_Markup_Extensions()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(true, false));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </UserControl.Resources>
    <Border Background='{OnFormFactor Default={StaticResource brush}}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = (Border)userControl.Content!;

            Assert.Equal(Color.Parse("#ff506070"), ((ISolidColorBrush)border.Background!).Color);
        }
    }

    [Fact]
    public void Should_Support_Xml_Syntax()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(true, false));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border>
        <Border.Background>
            <OnFormFactor>
                <OnFormFactor.Default>
                    <SolidColorBrush Color='#ff506070' />
                </OnFormFactor.Default>
            </OnFormFactor>
        </Border.Background>
    </Border>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = (Border)userControl.Content!;

            Assert.Equal(Color.Parse("#ff506070"), ((ISolidColorBrush)border.Background!).Color);
        }
    }

    [Fact]
    public void Should_Support_Xml_Syntax_With_Custom_TypeArguments()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(true, false));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border>
        <Border.Tag>
            <OnFormFactor x:TypeArguments='Thickness' Default='10, 10, 10, 10' />
        </Border.Tag>
    </Border>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = (Border)userControl.Content!;

            Assert.Equal(new Thickness(10, 10, 10, 10), border.Tag);
        }
    }

    [Fact]
    public void Should_Support_Control_Inside_Xml_Syntax()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(true, false));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <OnFormFactor>
        <OnFormFactor.Default>
            <Button Content='Hello World' />
        </OnFormFactor.Default>
    </OnFormFactor>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var button = (Button)userControl.Content!;

            Assert.Equal("Hello World", button.Content);
        }
    }

    [Fact]
    public void Should_Support_Complex_Property_Setters()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(true, false));

            var xaml = @"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <OnFormFactor x:Key='MyKey'>
        <OnFormFactor.Default>
            <Button Content='Hello World' />
        </OnFormFactor.Default>
    </OnFormFactor>
</ResourceDictionary>";

            var resourceDictionary = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(xaml);
            var button = Assert.IsType<Button>(resourceDictionary["MyKey"]);

            Assert.Equal("Hello World", button.Content);
        }
    }

    [Fact]
    public void BindingExtension_Works_Inside_Of_OnFormFactor()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(true, false));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <x:String x:Key='text'>foobar</x:String>
    </UserControl.Resources>

    <TextBlock Name='textBlock' Text='{OnFormFactor Default={CompiledBinding Source={StaticResource text}}}'/>
</UserControl>";

            var window = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var textBlock = window.FindControl<TextBlock>("textBlock");

            Assert.Equal("foobar", textBlock.Text);
        }
    }

    private class TestRuntimePlatform : StandardRuntimePlatform
    {
        private readonly bool _isDesktop;
        private readonly bool _isMobile;

        public TestRuntimePlatform(bool isDesktop, bool isMobile)
        {
            _isDesktop = isDesktop;
            _isMobile = isMobile;
        }

        public override RuntimePlatformInfo GetRuntimeInfo()
        {
            return new RuntimePlatformInfo() { IsDesktop = _isDesktop, IsMobile = _isMobile };
        }
    }
}
