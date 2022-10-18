using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;

public class OnPlatformExtensionTests : XamlTestBase
{
    [Fact]
    public void Should_Resolve_Default_Value()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(OperatingSystemType.Unknown));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Text='{OnPlatform Default=""Hello World""}'/>
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
                .ToConstant(new TestRuntimePlatform(OperatingSystemType.Unknown));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Text='{OnPlatform ""Hello World"", Android=""Im Android""}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var textBlock = (TextBlock)userControl.Content!;

            Assert.Equal("Hello World", textBlock.Text);
        }
    }

    [Theory]
    [InlineData(OperatingSystemType.WinNT, "Im Windows")]
    [InlineData(OperatingSystemType.OSX, "Im macOS")]
    [InlineData(OperatingSystemType.Linux, "Im Linux")]
    [InlineData(OperatingSystemType.Android, "Im Android")]
    [InlineData(OperatingSystemType.iOS, "Im iOS")]
    [InlineData(OperatingSystemType.Browser, "Im Browser")]
    [InlineData(OperatingSystemType.Unknown, "Default value")]
    public void Should_Resolve_Expected_Value_Per_Platform(OperatingSystemType currentPlatform, string expectedResult)
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(currentPlatform));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Text='{OnPlatform ""Default value"",
                                 Windows=""Im Windows"", macOS=""Im macOS"",
                                 Linux=""Im Linux"", Android=""Im Android"",
                                 iOS=""Im iOS"", Browser=""Im Browser""}'/>
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
                .ToConstant(new TestRuntimePlatform(OperatingSystemType.WinNT));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Height='{OnPlatform Windows=50.1}'/>
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
                .ToConstant(new TestRuntimePlatform(OperatingSystemType.WinNT));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Padding='{OnPlatform Windows=""10, 8, 10, 8""}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = (Border)userControl.Content!;

            Assert.Equal(new Thickness(10, 8, 10, 8), border.Padding);
        }
    }

    [Fact(Skip = "Fix me")]
    public void Should_Respect_Custom_TypeArgument()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(OperatingSystemType.WinNT));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock DataContext='{OnPlatform Windows=""10, 10, 10, 10"", x:TypeArguments=Thickness}'/>
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
                .ToConstant(new TestRuntimePlatform(OperatingSystemType.WinNT));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </UserControl.Resources>
    <Border Background='{OnPlatform Windows={StaticResource brush}}'/>
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
                .ToConstant(new TestRuntimePlatform(OperatingSystemType.WinNT));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border>
        <Border.Background>
            <OnPlatform>
                <OnPlatform.Windows>
                    <SolidColorBrush Color='#ff506070' />
                </OnPlatform.Windows>
            </OnPlatform>
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
                .ToConstant(new TestRuntimePlatform(OperatingSystemType.WinNT));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border>
        <Border.Tag>
            <OnPlatform x:TypeArguments='Thickness' Windows='10, 10, 10, 10' />
        </Border.Tag>
    </Border>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = (Border)userControl.Content!;

            Assert.Equal(new Thickness(10, 10, 10, 10), border.Tag);
        }
    }

    [Fact]
    public void Should_Support_Special_On_Syntax()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(OperatingSystemType.OSX));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border>
        <Border.Background>
            <OnPlatform>
                <On Platform='Windows, macOS'>
                    <SolidColorBrush Color='#ff506070' />
                </On>
                <On Platform='Linux'>
                    <SolidColorBrush Color='#000' />
                </On>
            </OnPlatform>
        </Border.Background>
    </Border>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = (Border)userControl.Content!;

            Assert.Equal(Color.Parse("#ff506070"), ((ISolidColorBrush)border.Background!).Color);
        }
    }

    [Fact]
    public void Should_Support_Control_Inside_Xml_Syntax()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(OperatingSystemType.WinNT));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <OnPlatform>
        <OnPlatform.Windows>
            <Button Content='Hello World' />
        </OnPlatform.Windows>
    </OnPlatform>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var button = (Button)userControl.Content!;

            Assert.Equal("Hello World", button.Content);
        }
    }

    private class TestRuntimePlatform : StandardRuntimePlatform
    {
        private readonly OperatingSystemType _operatingSystemType;

        public TestRuntimePlatform(OperatingSystemType operatingSystemType)
        {
            _operatingSystemType = operatingSystemType;
        }

        public override RuntimePlatformInfo GetRuntimeInfo()
        {
            return new RuntimePlatformInfo() { OperatingSystem = _operatingSystemType };
        }
    }
}

public class TestOnPlatformConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Assert.Equal("My Parameter", parameter);
        Assert.Equal("My Value", value);
        Assert.Equal(typeof(Thickness), targetType);

        return new Thickness(4);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
