using Avalonia.Controls;
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
