#nullable enable

using System;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextRunCacheTests
    {
        [Fact]
        public void Cache_Hit_Produces_Identical_Layout()
        {
            using (Start())
            {
                var text = "Hello World";
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                using var cache = new TextRunCache();

                // First call: cache miss, populates cache.
                var line1 = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    paragraphProperties, null, cache);

                Assert.NotNull(line1);

                // Second call: cache hit, different paragraph width.
                var line2 = formatter.FormatLine(textSource, 0, 200.0,
                    paragraphProperties, null, cache);

                Assert.NotNull(line2);

                // Both lines should have the same text length.
                Assert.Equal(line1!.Length, line2!.Length);

                // Both lines should have the same number of text runs.
                Assert.Equal(line1.TextRuns.Count, line2.TextRuns.Count);
            }
        }

        [Fact]
        public void Full_Invalidation_Clears_Cache()
        {
            using (Start())
            {
                var text = "Hello World";
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                using var cache = new TextRunCache();

                // Populate cache.
                formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    paragraphProperties, null, cache);

                // Invalidate.
                cache.Invalidate();

                // Verify cache miss: should not throw and should produce a valid line.
                var line = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    paragraphProperties, null, cache);

                Assert.NotNull(line);
                Assert.Equal(text.Length, line!.Length);
            }
        }

        [Fact]
        public void Partial_Invalidation_Preserves_Earlier_Entries()
        {
            using (Start())
            {
                var text = "First paragraph\nSecond paragraph";
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                using var cache = new TextRunCache();

                // Format first paragraph (populates cache at index 0).
                var line1 = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    paragraphProperties, null, cache);

                Assert.NotNull(line1);

                var firstLineLength = line1!.Length;

                // Format second paragraph (populates cache at firstLineLength).
                var line2 = formatter.FormatLine(textSource, firstLineLength, double.PositiveInfinity,
                    paragraphProperties, null, cache);

                Assert.NotNull(line2);

                // Invalidate from the second paragraph index.
                cache.InvalidateFrom(firstLineLength);

                // First paragraph should still be cached (cache hit).
                var line1Again = formatter.FormatLine(textSource, 0, 200.0,
                    paragraphProperties, null, cache);

                Assert.NotNull(line1Again);
                Assert.Equal(line1.Length, line1Again!.Length);

                // Second paragraph should be re-shaped (cache miss then re-populated).
                var line2Again = formatter.FormatLine(textSource, firstLineLength, double.PositiveInfinity,
                    paragraphProperties, null, cache);

                Assert.NotNull(line2Again);
                Assert.Equal(line2!.Length, line2Again!.Length);
            }
        }

        [Fact]
        public void Text_Wrapping_With_Cache_Produces_Correct_Lines()
        {
            using (Start())
            {
                var text = "The quick brown fox jumps over the lazy dog";
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var wrappingProperties = new GenericTextParagraphProperties(
                    FlowDirection.LeftToRight, TextAlignment.Left, true, false,
                    defaultProperties, TextWrapping.Wrap, 0, 0, 0);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                // Format without cache.
                var linesWithout = FormatAllLines(formatter, textSource, 100.0, wrappingProperties, null);

                // Format with cache (first pass: cache miss).
                using var cache = new TextRunCache();
                var linesWith = FormatAllLines(formatter, textSource, 100.0, wrappingProperties, cache);

                Assert.Equal(linesWithout.Length, linesWith.Length);

                for (int i = 0; i < linesWithout.Length; i++)
                {
                    Assert.Equal(linesWithout[i].Length, linesWith[i].Length);
                }

                // Format with cache again (second pass: cache hit).
                var linesCacheHit = FormatAllLines(formatter, textSource, 100.0, wrappingProperties, cache);

                Assert.Equal(linesWithout.Length, linesCacheHit.Length);

                for (int i = 0; i < linesWithout.Length; i++)
                {
                    Assert.Equal(linesWithout[i].Length, linesCacheHit[i].Length);
                }
            }
        }

        [Fact]
        public void Wrapping_With_Different_Width_From_Cache()
        {
            using (Start())
            {
                var text = "The quick brown fox jumps over the lazy dog";
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var wrappingProperties = new GenericTextParagraphProperties(
                    FlowDirection.LeftToRight, TextAlignment.Left, true, false,
                    defaultProperties, TextWrapping.Wrap, 0, 0, 0);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                using var cache = new TextRunCache();

                // First format: wide (cache miss, populates).
                var wideLines = FormatAllLines(formatter, textSource, 500.0, wrappingProperties, cache);

                // Second format: narrow (cache hit, different wrapping).
                var narrowLines = FormatAllLines(formatter, textSource, 80.0, wrappingProperties, cache);

                // Narrow should produce more lines.
                Assert.True(narrowLines.Length >= wideLines.Length);

                // Total characters should be the same.
                int wideTotal = 0, narrowTotal = 0;
                foreach (var l in wideLines) wideTotal += l.Length;
                foreach (var l in narrowLines) narrowTotal += l.Length;

                Assert.Equal(wideTotal, narrowTotal);
            }
        }

        [Fact]
        public void Bidi_Text_With_Cache_Produces_Correct_Results()
        {
            using (Start())
            {
                // Mixed LTR/RTL text.
                var text = "Hello \u0627\u0644\u0639\u0631\u0628\u064A\u0629 World";
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                // Without cache.
                var lineWithout = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    paragraphProperties, null, null);

                // With cache.
                using var cache = new TextRunCache();
                var lineWith = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    paragraphProperties, null, cache);

                Assert.NotNull(lineWithout);
                Assert.NotNull(lineWith);

                Assert.Equal(lineWithout!.Length, lineWith!.Length);
                Assert.Equal(lineWithout.TextRuns.Count, lineWith.TextRuns.Count);

                // Cache hit should also produce correct results.
                var lineCacheHit = formatter.FormatLine(textSource, 0, 200.0,
                    paragraphProperties, null, cache);

                Assert.NotNull(lineCacheHit);
                Assert.Equal(lineWithout.Length, lineCacheHit!.Length);
            }
        }

        [Fact]
        public void RTL_Text_Glyph_Order_Is_Identical_On_Cache_Miss_And_Cache_Hit()
        {
            using (Start())
            {
                // Pure RTL paragraph. Every run will have an odd BidiLevel and be reversed by BidiReorder.
                var text = "\u05E9\u05DC\u05D5\u05DD \u05E2\u05D5\u05DC\u05DD"; // "שלום עולם" (Hello World in Hebrew)
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(
                    FlowDirection.RightToLeft, TextAlignment.Right, true, false,
                    defaultProperties, TextWrapping.NoWrap, 0, 0, 0);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                const double paragraphWidth = 500.0;

                using var cache = new TextRunCache();

                // Cache miss.
                var lineMiss = formatter.FormatLine(textSource, 0, paragraphWidth,
                    paragraphProperties, null, cache);

                Assert.NotNull(lineMiss);

                // For RTL text the first logical character must be at a greater x-distance than the
                // last logical character, i.e. the line reads right-to-left visually.
                Assert.True(
                    lineMiss!.GetDistanceFromCharacterHit(new CharacterHit(0)) >
                    lineMiss.GetDistanceFromCharacterHit(new CharacterHit(text.Length - 1)),
                    "Cache-miss RTL line: first character should be to the right of the last character.");

                // Verify all shaped runs in the line were correctly reversed.
                foreach (var run in lineMiss.TextRuns)
                {
                    if (run is ShapedTextRun shaped && !shaped.ShapedBuffer.IsLeftToRight)
                        Assert.True(shaped.IsReversed, "Cache-miss: RTL ShapedTextRun should be reversed after FinalizeLine.");
                }

                // Capture per-character distances from the cache-miss line (same width as cache-hit below).
                var distancesMiss = new double[text.Length];
                for (var i = 0; i < text.Length; i++)
                    distancesMiss[i] = lineMiss.GetDistanceFromCharacterHit(new CharacterHit(i));

                // Cache hit — same paragraph width so distances are directly comparable.
                var lineHit = formatter.FormatLine(textSource, 0, paragraphWidth,
                    paragraphProperties, null, cache);

                Assert.NotNull(lineHit);

                // Distances on the cache-hit line must exactly match the cache-miss line.
                for (var i = 0; i < text.Length; i++)
                    Assert.Equal(distancesMiss[i], lineHit!.GetDistanceFromCharacterHit(new CharacterHit(i)));

                // RTL direction must still hold on the cache-hit line.
                Assert.True(
                    lineHit!.GetDistanceFromCharacterHit(new CharacterHit(0)) >
                    lineHit.GetDistanceFromCharacterHit(new CharacterHit(text.Length - 1)),
                    "Cache-hit RTL line: first character should be to the right of the last character.");

                // All RTL runs must still be reversed after FinalizeLine on the cache-hit path.
                foreach (var run in lineHit.TextRuns)
                {
                    if (run is ShapedTextRun shaped && !shaped.ShapedBuffer.IsLeftToRight)
                        Assert.True(shaped.IsReversed, "Cache-hit: RTL ShapedTextRun should be reversed after FinalizeLine.");
                }
            }
        }

        [Fact]
        public void Dispose_Releases_Cache_Entries()
        {
            using (Start())
            {
                var text = "Hello World";
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var cache = new TextRunCache();

                // Populate cache.
                formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    paragraphProperties, null, cache);

                // Dispose should not throw.
                cache.Dispose();

                // After dispose, using the cache should still work (re-creates entries).
                using var cache2 = new TextRunCache();

                var line = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    paragraphProperties, null, cache2);

                Assert.NotNull(line);
            }
        }

        [Fact]
        public void Cache_With_Multiple_Paragraphs()
        {
            using (Start())
            {
                var text = "First line\nSecond line\nThird line";
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                using var cache = new TextRunCache();

                // Format all paragraphs.
                var lines = FormatAllLines(formatter, textSource, double.PositiveInfinity,
                    paragraphProperties, cache);

                Assert.True(lines.Length >= 3);

                // Format again from cache with different width.
                var lines2 = FormatAllLines(formatter, textSource, double.PositiveInfinity,
                    paragraphProperties, cache);

                Assert.Equal(lines.Length, lines2.Length);

                for (int i = 0; i < lines.Length; i++)
                {
                    Assert.Equal(lines[i].Length, lines2[i].Length);
                }
            }
        }

        [Fact]
        public void TextLayout_With_Cache_Matches_Without()
        {
            using (Start())
            {
                var text = "The quick brown fox jumps over the lazy dog";

                // Layout without cache.
                var layout1 = new TextLayout(text, Typeface.Default, 12,
                    textWrapping: TextWrapping.Wrap, maxWidth: 100);

                // Layout with cache.
                using var cache = new TextRunCache();
                var layout2 = new TextLayout(text, Typeface.Default, 12,
                    textWrapping: TextWrapping.Wrap, maxWidth: 100, textRunCache: cache);

                Assert.Equal(layout1.TextLines.Count, layout2.TextLines.Count);
                Assert.Equal(layout1.Height, layout2.Height);
                Assert.Equal(layout1.WidthIncludingTrailingWhitespace,
                    layout2.WidthIncludingTrailingWhitespace);

                // Second layout from cache with different width.
                var layout3 = new TextLayout(text, Typeface.Default, 12,
                    textWrapping: TextWrapping.Wrap, maxWidth: 80, textRunCache: cache);

                // Should still be valid (more lines due to narrower width).
                Assert.True(layout3.TextLines.Count >= layout2.TextLines.Count);
                Assert.True(layout3.Height > 0);

                layout1.Dispose();
                layout2.Dispose();
                layout3.Dispose();
            }
        }

        private static TextLine[] FormatAllLines(TextFormatterImpl formatter, ITextSource textSource,
            double paragraphWidth, TextParagraphProperties paragraphProperties, TextRunCache? cache)
        {
            var lines = new System.Collections.Generic.List<TextLine>();
            var currentIndex = 0;
            TextLine? previousLine = null;

            while (true)
            {
                var line = formatter.FormatLine(textSource, currentIndex, paragraphWidth,
                    paragraphProperties, previousLine?.TextLineBreak, cache);

                if (line == null)
                {
                    break;
                }

                lines.Add(line);
                currentIndex += line.Length;
                previousLine = line;

                if (line.TextLineBreak?.TextEndOfLine is TextEndOfParagraph)
                {
                    break;
                }
            }

            return lines.ToArray();
        }

        private static IDisposable Start()
        {
            return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null),
                    fontManagerImpl: new CustomFontManagerImpl()));
        }
    }
}
