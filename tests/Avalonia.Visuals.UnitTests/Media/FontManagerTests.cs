using System;
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
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var fontFamily = new FontFamily("MyFont");

                var typeface = FontManager.Current.GetOrAddTypeface(fontFamily);

                Assert.Same(typeface, FontManager.Current.GetOrAddTypeface(fontFamily));
            }
        }

        [Fact]
        public void Should_Throw_When_Default_FamilyName_Is_Null()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new MockFontManagerImpl(null))))
            {
                Assert.Throws<InvalidOperationException>(() => FontManager.Current);
            }
        }
    }
}
