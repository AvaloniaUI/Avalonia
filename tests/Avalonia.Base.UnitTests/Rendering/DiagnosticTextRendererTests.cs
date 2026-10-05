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

        private static double MeasureGlyph(char c)
        {
            var glyphTypeface = Typeface.Default.GlyphTypeface;
            var glyph = glyphTypeface.CharacterToGlyphMap[c];
            return new GlyphRun(glyphTypeface, EmSize, c.ToString().AsMemory(), new[] { glyph }).Bounds.Width;
        }

        private static DiagnosticTextRenderer CreateRenderer()
            => new(Typeface.Default.GlyphTypeface, EmSize);

        private static List<double> Draw(DiagnosticTextRenderer renderer, string text)
        {
            var drawn = new List<double>();
            var impl = new Mock<IDrawingContextImpl>();
            var transform = Matrix.Identity;
            impl.SetupGet(x => x.Transform).Returns(() => transform);
            impl.SetupSet(x => x.Transform = It.IsAny<Matrix>()).Callback<Matrix>(m => transform = m);
            impl.Setup(x => x.DrawGlyphRun(It.IsAny<IBrush?>(), It.IsAny<IGlyphRunImpl>()))
                .Callback<IBrush?, IGlyphRunImpl>((_, _) => drawn.Add(transform.M31));

            using (var context = new ImmediateDrawingContext(impl.Object, false))
            {
                renderer.DrawAsciiText(context, text.AsSpan(), Brushes.White);
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
