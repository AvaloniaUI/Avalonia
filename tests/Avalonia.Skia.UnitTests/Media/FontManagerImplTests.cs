﻿using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class FontManagerImplTests
    {
        private static string s_fontUri = "resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono";

        [Fact]
        public void Should_Create_Typeface_From_Fallback()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var fontManager = FontManager.Current;

                var glyphTypeface = new Typeface(new FontFamily("A, B, " + fontManager.DefaultFontFamilyName)).GlyphTypeface;

                Assert.Equal(SKTypeface.Default.FamilyName, glyphTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Create_Typeface_From_Fallback_Bold()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var glyphTypeface = new Typeface(new FontFamily($"A, B, Arial"), weight: FontWeight.Bold).GlyphTypeface;

                Assert.True((int)glyphTypeface.Weight >= 600);
            }
        }

        [Fact]
        public void Should_Yield_Default_GlyphTypeface_For_Invalid_FamilyName()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {           
               var glyphTypeface = new Typeface(new FontFamily("Unknown")).GlyphTypeface;

                Assert.Equal(FontManager.Current.DefaultFontFamilyName, glyphTypeface.FamilyName);             
            }
        }

        [Fact]
        public void Should_Load_Typeface_From_Resource()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var glyphTypeface = new Typeface(s_fontUri).GlyphTypeface;

                Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Load_Nearest_Matching_Font()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                var glyphTypeface = new Typeface(s_fontUri, FontStyle.Italic, FontWeight.Black).GlyphTypeface;

                Assert.Equal("Noto Mono", glyphTypeface.FamilyName);
            }
        }

        [Fact]
        public void Should_Throw_For_Invalid_Custom_Font()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: new FontManagerImpl())))
            {
                Assert.Throws<InvalidOperationException>(() => new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Unknown").GlyphTypeface);
            }
        }
    }
}
