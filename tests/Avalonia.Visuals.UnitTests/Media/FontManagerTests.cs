using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class FontManagerTests
    {
        [Fact]
        public void Should_Change_Current_When_Platform_Implementation_Changes()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var current = FontManager.Current;

                var fontManager = new MockFontManagerImpl();

                AvaloniaLocator.CurrentMutable.Bind<IFontManagerImpl>().ToConstant(fontManager);

                Assert.NotEqual(current, FontManager.Current);
            }
        }
    }
}
