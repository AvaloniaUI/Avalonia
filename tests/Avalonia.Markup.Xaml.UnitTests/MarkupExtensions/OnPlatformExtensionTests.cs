using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.PlatformSupport;
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
    public void Should_Use_Converter_If_Provided()
    {
        using (AvaloniaLocator.EnterScope())
        {
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>()
                .ToConstant(new TestRuntimePlatform(OperatingSystemType.WinNT));

            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <UserControl.Resources>
        <local:TestOnPlatformConverter x:Key='TestConverter' />
    </UserControl.Resources>
    <Border Padding='{OnPlatform Windows=""My Value"", ConverterParameter=""My Parameter"", Converter={StaticResource TestConverter}}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = (Border)userControl.Content!;

            Assert.Equal(new Thickness(4), border.Padding);
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
