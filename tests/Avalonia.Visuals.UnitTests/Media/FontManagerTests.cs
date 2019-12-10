using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class FontManagerTests
    {
        [Fact]
        public void Should_Create_Single_Instance_Typeface()
        {
            using (AvaloniaLocator.EnterScope())
            {
                AvaloniaLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(new MockPlatformRenderInterface());

                var fontFamily = new FontFamily("MyFont");

                var typeface = FontManager.Current.GetOrAddTypeface(fontFamily);

                Assert.Same(typeface, FontManager.Current.GetOrAddTypeface(fontFamily));
            }
        }
    }
}
