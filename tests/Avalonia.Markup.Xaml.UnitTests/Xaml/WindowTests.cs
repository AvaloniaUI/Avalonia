using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class WindowTests : XamlTestBase
    {
        [Fact]
        public void Can_Specify_TransparencyLevelHint()
        {
            using var app = UnitTestApplication.Start(TestServices.MockWindowingPlatform);
            var xaml = @"<Window xmlns='https://github.com/avaloniaui' TransparencyLevelHint='Blur,Transparent,None'/>";

            var target = AvaloniaRuntimeXamlLoader.Parse<Window>(xaml);

            Assert.Equal(
                new[]
                {
                    WindowTransparencyLevel.Blur,
                    WindowTransparencyLevel.Transparent,
                    WindowTransparencyLevel.None,
                }, target.TransparencyLevelHint);
        }
    }
}
