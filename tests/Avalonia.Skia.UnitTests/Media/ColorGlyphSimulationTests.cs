#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    /// <summary>
    /// A colour font face carries the simulations of its request, and the renderer decides what they
    /// do to its colour glyphs. Skia's fake bold emboldens the outline glyphs of a COLR face and
    /// leaves its colour layers as designed.
    /// </summary>
    public class ColorGlyphSimulationTests
    {
        private const float EmSize = 64;

        private const int Width = 400;

        private const int Height = 120;

        private const string EmojiFamily = "Segoe UI Emoji";

        [Win32Fact("Windows specific font")]
        public void Bold_Simulation_Should_Leave_Colour_Glyphs_As_Designed()
        {
            using var faces = new Faces(EmojiFamily, FontSimulations.Bold);

            const string text = "\U0001F600❤\U0001F44D";

            using var actual = Render(faces.Simulated, text);
            using var expected = Render(faces.Designed, text);

            AssertSamePixels(expected, actual);
        }

        [Win32Fact("Windows specific font")]
        public void Bold_Simulation_Should_Embolden_The_Outline_Glyphs_Of_A_Colour_Font_Face()
        {
            using var faces = new Faces(EmojiFamily, FontSimulations.Bold);

            const string text = "123";

            using var actual = Render(faces.Simulated, text);
            using var designed = Render(faces.Designed, text);

            Assert.False(SamePixels(designed, actual));
        }

        /// <summary>The simulated face and the face as designed, both of one installed family.</summary>
        private sealed class Faces : IDisposable
        {
            public Faces(string familyName, FontSimulations simulations)
            {
                Simulated = CreateTypeface(familyName, simulations);
                Designed = CreateTypeface(familyName, FontSimulations.None);
            }

            public GlyphTypeface Simulated { get; }

            public GlyphTypeface Designed { get; }

            public void Dispose()
            {
                Simulated.Dispose();
                Designed.Dispose();
            }
        }

        private static GlyphTypeface CreateTypeface(string familyName, FontSimulations simulations)
        {
            var skTypeface = SKTypeface.FromFamilyName(familyName);

            Assert.Equal(familyName, skTypeface.FamilyName);

            return new GlyphTypeface(new SkiaTypeface(skTypeface, simulations), simulations);
        }

        private static List<GlyphInfo> Glyphs(GlyphTypeface typeface, string text)
        {
            var scale = EmSize / typeface.Metrics.DesignEmHeight;
            var glyphs = new List<GlyphInfo>();

            for (var i = 0; i < text.Length; i += char.IsSurrogatePair(text, i) ? 2 : 1)
            {
                var codepoint = char.ConvertToUtf32(text, i);

                Assert.True(typeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out var glyph));
                Assert.True(typeface.TryGetHorizontalGlyphAdvance(glyph, out var advance));

                glyphs.Add(new GlyphInfo(glyph, i, advance * scale));
            }

            return glyphs;
        }

        private static SKBitmap Render(GlyphTypeface typeface, string text)
        {
            var bitmap = new SKBitmap(Width, Height);

            using var canvas = new SKCanvas(bitmap);
            using var paint = new SKPaint { Color = SKColors.Black };
            using var glyphRun = new GlyphRunImpl(typeface, EmSize, Glyphs(typeface, text), default);

            var textOptions = default(TextOptions) with { TextRenderingMode = TextRenderingMode.Antialias };

            canvas.Clear(SKColors.White);
            canvas.DrawText(glyphRun.GetTextBlob(textOptions, default), 20, 90, paint);

            return bitmap;
        }

        private static bool SamePixels(SKBitmap expected, SKBitmap actual)
            => expected.GetPixelSpan().SequenceEqual(actual.GetPixelSpan());

        private static void AssertSamePixels(SKBitmap expected, SKBitmap actual)
        {
            if (SamePixels(expected, actual))
            {
                return;
            }

            var directory = Path.Combine(Path.GetTempPath(), nameof(ColorGlyphSimulationTests));

            Directory.CreateDirectory(directory);

            Save(expected, Path.Combine(directory, "expected.png"));
            Save(actual, Path.Combine(directory, "actual.png"));

            Assert.Fail($"The glyph run differs from its expected rendering, written to {directory}.");
        }

        private static void Save(SKBitmap bitmap, string path)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(path);

            data.SaveTo(file);
        }
    }
}
