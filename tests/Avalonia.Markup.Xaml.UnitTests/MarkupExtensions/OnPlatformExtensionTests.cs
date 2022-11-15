using Avalonia.Controls;
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
