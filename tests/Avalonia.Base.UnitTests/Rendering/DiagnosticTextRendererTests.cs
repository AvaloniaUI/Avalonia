using System;
using System.Collections.Generic;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering
{
    public class DiagnosticTextRendererTests : TestWithServicesBase
    {
        private const double EmSize = 12.0;

        [Fact]
        public void Digits_Measure_To_The_Same_Width()
        {
            using (Start())
            {
                var renderer = CreateRenderer();

                var width = renderer.MeasureAsciiText("0".AsSpan()).Width;

                for (var c = '1'; c <= '9'; c++)
                {
                    Assert.Equal(width, renderer.MeasureAsciiText(c.ToString().AsSpan()).Width);
                }

                Assert.Equal(width * 3, renderer.MeasureAsciiText("108".AsSpan()).Width);
            }
        }

        [Fact]
        public void Height_Is_The_Scaled_Line_Spacing()
        {
            using (Start())
            {
                var glyphTypeface = Typeface.Default.GlyphTypeface;
                var renderer = new DiagnosticTextRenderer(glyphTypeface, EmSize);
                var expected = glyphTypeface.Metrics.LineSpacing * EmSize / glyphTypeface.Metrics.DesignEmHeight;

                Assert.Equal(expected, renderer.GetMaxHeight());
                Assert.Equal(expected, renderer.MeasureAsciiText("FPS: 60".AsSpan()).Height);
            }
        }

        [Fact]
        public void Digits_Are_Centered_Inside_The_Digit_Cell()
        {
            using (Start())
            {
                var renderer = CreateRenderer();
                var cellWidth = renderer.MeasureAsciiText("0".AsSpan()).Width;

                // Narrow digits shift to the center of their cell, they must never leave it.
                const string digits = "1234567890";
                var drawn = Draw(renderer, digits);

                Assert.Equal(digits.Length, drawn.Count);

                for (var i = 0; i < drawn.Count; i++)
                {
                    var cellStart = i * cellWidth;
                    var glyphWidth = MeasureGlyph(digits[i]);

                    Assert.True(drawn[i] >= cellStart - 1e-6, $"Digit {i} starts before its cell.");
                    Assert.True(drawn[i] + glyphWidth <= cellStart + cellWidth + 1e-6, $"Digit {i} ends after its cell.");
                    Assert.Equal(cellStart + (cellWidth - glyphWidth) / 2.0, drawn[i], 6);
                }
            }
        }

        [Fact]
        public void Text_After_Digits_Does_Not_Depend_On_The_Digit_Values()
        {
            using (Start())
            {
                var renderer = CreateRenderer();

                var narrow = Draw(renderer, "111 ms");
                var wide = Draw(renderer, "888 ms");

                Assert.Equal(narrow.Count, wide.Count);
                Assert.Equal(narrow[^1], wide[^1], 6);
            }
        }

        [Theory]
        [InlineData(42, 5, "00042")]
        [InlineData(0, 3, "000")]
        [InlineData(12345678, 8, "12345678")]
        [InlineData(1000, 3, "999")]
        [InlineData(-5, 3, "000")]
        public void DrawDigits_Matches_Drawing_The_Padded_Text(int value, int digitCount, string expectedText)
        {
            using (Start())
            {
                var renderer = CreateRenderer();

                var digits = Capture(c => renderer.DrawDigits(c, value, digitCount, Brushes.White));
                var text = Capture(c => renderer.DrawAsciiText(c, expectedText.AsSpan(), Brushes.White));

                Assert.Equal(text.Count, digits.Count);

                for (var i = 0; i < text.Count; i++)
                {
                    Assert.Same(text[i].Glyph, digits[i].Glyph);
                    Assert.Equal(text[i].X, digits[i].X, 6);
                }
            }
        }

        [Fact]
        public void ShapedText_Is_Measured_Once_And_Drawn_As_A_Single_Run()
        {
            using (Start())
            {
                var renderer = CreateRenderer();
                using var shaped = renderer.Shape(" FPS: ");

                Assert.Equal(renderer.MeasureAsciiText(" FPS: ".AsSpan()).Width, shaped.Width, 6);
                Assert.Single(Capture(c => renderer.DrawShapedText(c, shaped, Brushes.White)));
            }
        }

        [Fact]
        public void FpsCounter_Layout_Does_Not_Depend_On_The_Numbers()
        {
            using (Start())
            {
                var counter = new FpsCounter(CreateRenderer());
                var rects = new List<Rect?>();

                var first = Capture(c => rects.Add(counter.RenderFps(c, 1000, 1000, 1, 1, false, null)));
                var second = Capture(c => rects.Add(counter.RenderFps(c, 1000, 1000, 99999, 88888, false, null)));

                Assert.Equal(rects[0], rects[1]);
                Assert.Equal(first.Count, second.Count);

                // Labels, and every digit cell, stay where they were no matter which digits are shown.
                for (var i = 0; i < first.Count; i++)
                {
                    if (first[i].Glyph.Equals(second[i].Glyph))
                        Assert.Equal(first[i].X, second[i].X, 6);
                }
            }
        }

        private static double MeasureGlyph(char c)
        {
            var glyphTypeface = Typeface.Default.GlyphTypeface;
            var glyph = glyphTypeface.CharacterToGlyphMap[c];
            return new GlyphRun(glyphTypeface, EmSize, c.ToString().AsMemory(), new[] { glyph }).Bounds.Width;
        }

        private static DiagnosticTextRenderer CreateRenderer()
            => new(Typeface.Default.GlyphTypeface, EmSize);

        private static List<double> Draw(DiagnosticTextRenderer renderer, string text)
            => Capture(context => renderer.DrawAsciiText(context, text.AsSpan(), Brushes.White)).ConvertAll(d => d.X);

        private static List<(double X, IGlyphRunImpl Glyph)> Capture(Action<ImmediateDrawingContext> draw)
        {
            var drawn = new List<(double, IGlyphRunImpl)>();
            var impl = new Mock<IDrawingContextImpl>();
            var transform = Matrix.Identity;
            impl.SetupGet(x => x.Transform).Returns(() => transform);
            impl.SetupSet(x => x.Transform = It.IsAny<Matrix>()).Callback<Matrix>(m => transform = m);
            impl.Setup(x => x.DrawGlyphRun(It.IsAny<IBrush?>(), It.IsAny<IGlyphRunImpl>()))
                .Callback<IBrush?, IGlyphRunImpl>((_, glyph) => drawn.Add((transform.M31, glyph)));

            using (var context = new ImmediateDrawingContext(impl.Object, false))
            {
                draw(context);
            }

            return drawn;
        }

        private static IDisposable Start()
        {
            return UnitTestApplication.Start(TestServices.StyledWindow.With(
                renderInterface: new HeadlessPlatformRenderInterface()));
        }
    }
}
