using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class FontManagerTests
    {
        [Fact]
        public void Should_Create_Single_Instance_Typeface()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var fontFamily = new FontFamily("MyFont");

                var typeface = new Typeface(fontFamily);

                Assert.True(FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface));

                FontManager.Current.TryGetGlyphTypeface(typeface, out var other);

                Assert.Same(glyphTypeface, other);
            }
        }

        [Fact]
        public void Should_Throw_When_Default_FamilyName_Is_Null_And_Installed_Font_Family_Names_Is_Empty()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
               .With(fontManagerImpl: new HeadlessFontManagerWithMultipleSystemFontsStub(
                   installedFontFamilyNames: [],
                   defaultFamilyName: null!))))
            {
                Assert.Throws<InvalidOperationException>(() => FontManager.Current);
            }
        }

        [Fact]
        public void Should_Use_FontManagerOptions_DefaultFamilyName()
        {
            var options = new FontManagerOptions { DefaultFamilyName = "MyFont" };

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(fontManagerImpl: new HeadlessFontManagerStub())))
            {
                AvaloniaLocator.CurrentMutable.Bind<FontManagerOptions>().ToConstant(options);

                Assert.Equal("MyFont", FontManager.Current.DefaultFontFamily.Name);
            }
        }

        [Fact]
        public void Should_Use_FontManagerOptions_FontFallback()
        {
            var options = new FontManagerOptions
            {
                FontFallbacks = new[]
                {
                    new FontFallback
                    {
                        FontFamily = new FontFamily("MyFont"), UnicodeRange = UnicodeRange.Default
                    }
                }
            };

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                AvaloniaLocator.CurrentMutable.Bind<FontManagerOptions>().ToConstant(options);

                FontManager.Current.TryMatchCharacter('A', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
                    FontFamily.Default, null, out var typeface);

                Assert.Equal("MyFont", typeface.FontFamily.Name);
            }
        }

        [Fact]
        public void Should_Return_First_Installed_Font_Family_Name_When_Default_Family_Name_Is_Null()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(fontManagerImpl: new HeadlessFontManagerWithMultipleSystemFontsStub(
                    installedFontFamilyNames: ["DejaVu", "Verdana"],
                    defaultFamilyName: null!))))
            {
                Assert.Equal("DejaVu", FontManager.Current.DefaultFontFamily.Name);
            }
        }

        [Fact]
        public async Task TryGetGlyphTypeface_Should_Be_Thread_Safe_For_Embedded_Fonts()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var fontManager = FontManager.Current;
        
                const string fontUri =
                    "resm:Avalonia.Base.UnitTests.Assets?assembly=Avalonia.Base.UnitTests#Noto Mono";
                var collectionKey =
                    new Uri("resm:Avalonia.Base.UnitTests.Assets?assembly=Avalonia.Base.UnitTests");

                // Warm up to validate the font URI is correct.
                Assert.True(fontManager.TryGetGlyphTypeface(new Typeface(new FontFamily(fontUri)), out _));

                const int iterations = 50;
                int failures = 0;

                for (int i = 0; i < iterations; i++)
                {
                    fontManager.RemoveFontCollection(collectionKey);

                    using var barrier = new Barrier(2);
                    bool r1 = false, r2 = false;

                    var t1 = Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        r1 = fontManager.TryGetGlyphTypeface(new Typeface(new FontFamily(fontUri)), out _);
                    }, TestContext.Current.CancellationToken);

                    var t2 = Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        r2 = fontManager.TryGetGlyphTypeface(new Typeface(new FontFamily(fontUri)), out _);
                    }, TestContext.Current.CancellationToken);

                    await Task.WhenAll(t1, t2);

                    if (!r1 || !r2)
                    {
                        Interlocked.Increment(ref failures);
                    }
                }

                Assert.Equal(0, failures);
            }
        }
    }
}
