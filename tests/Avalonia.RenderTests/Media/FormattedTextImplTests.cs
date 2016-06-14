// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.
using Avalonia.Media;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;

#if AVALONIA_CAIRO
namespace Avalonia.Cairo.RenderTests.Media
#elif AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else

using Avalonia.Direct2D1.RenderTests;

namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class FormattedTextImplTests : TestBase
    {
        private const string FontName = "Courier New";
        private const double FontSize = 12;
        private const double FontSizeHeight = 13.594;//real value 13.59375
        private const string stringword = "word";
        private const string stringmiddle = "The quick brown fox jumps over the lazy dog";
        private const string stringmiddle2lines = "The quick brown fox\njumps over the lazy dog";
        private const string stringmiddle3lines = "The quick brown fox\n\njumps over the lazy dog";
        private const string stringmiddlenewlines = "012345678\r 1234567\r\n 12345678\n0123456789";

        private const string stringlong =
"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus magna. Cras in mi at felis " +
"aliquet congue. Ut a est eget ligula molestie gravida. Curabitur massa. Donec eleifend, libero" +
" at sagittis mollis, tellus est malesuada tellus, at luctus turpis elit sit amet quam. Vivamus " +
"pretium ornare est.";

        public FormattedTextImplTests()
            : base(@"Media\FormattedText")
        {
        }

        private IFormattedTextImpl Create(string text,
            string fontFamily,
            double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment,
            FontWeight fontWeight,
            TextWrapping wrapping)
        {
            var r = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            return r.CreateFormattedText(text,
                fontFamily,
                fontSize,
                fontStyle,
                textAlignment,
                fontWeight,
                wrapping);
        }

        private IFormattedTextImpl Create(string text, double fontSize)
        {
            return Create(text, FontName, fontSize,
                FontStyle.Normal, TextAlignment.Left,
                FontWeight.Normal, TextWrapping.NoWrap);
        }

        private IFormattedTextImpl Create(string text, double fontSize, TextWrapping wrap)
        {
            return Create(text, FontName, fontSize,
                FontStyle.Normal, TextAlignment.Left,
                FontWeight.Normal, wrap);
        }

#if AVALONIA_CAIRO
        [Theory(Skip = "TODO: Font scaling currently broken on cairo")]
#else
        [Theory]
#endif
        [InlineData("", 0, FontSizeHeight)]
        [InlineData("x", 7.20, FontSizeHeight)]
        [InlineData(stringword, 28.80, FontSizeHeight)]
        [InlineData(stringmiddle, 309.65, FontSizeHeight)]
        [InlineData(stringmiddle2lines, 165.63, 2 * FontSizeHeight)]
        [InlineData(stringlong, 2160.35, FontSizeHeight)]
        [InlineData(stringmiddlenewlines, 72.01, 4 * FontSizeHeight)]
        public void Should_Measure_String_Correctly(string input, double expWidth, double expHeight)
        {
#if !AVALONIA_SKIA
            double heightCorr = 0;
#else
            //In skia there is a small descent added to last line,
            //otherwise some letters are clipped at bottom
            //4.55273438 for font 12 size
            double heightCorr = 0.3793945*FontSize;
#endif
            using (var fmt = Create(input, FontSize))
            {
                var size = fmt.Measure();

                Assert.Equal(expWidth, size.Width, 2);
                Assert.Equal(expHeight + heightCorr, size.Height, 2);

                var linesHeight = fmt.GetLines().Sum(l => l.Height);

                Assert.Equal(expHeight, linesHeight, 2);
            }
        }

#if AVALONIA_CAIRO
        [Theory(Skip = "TODO: Font scaling currently broken on cairo")]
#else
        [Theory]
#endif
        [InlineData("", 1, -1, TextWrapping.NoWrap)]
        [InlineData("x", 1, -1, TextWrapping.NoWrap)]
        [InlineData(stringword, 1, -1, TextWrapping.NoWrap)]
        [InlineData(stringmiddle, 1, -1, TextWrapping.NoWrap)]
        [InlineData(stringmiddle, 3, 150, TextWrapping.Wrap)]
        [InlineData(stringmiddle2lines, 2, -1, TextWrapping.NoWrap)]
        [InlineData(stringmiddle2lines, 3, 150, TextWrapping.Wrap)]
        [InlineData(stringlong, 1, -1, TextWrapping.NoWrap)]
        [InlineData(stringlong, 18, 150, TextWrapping.Wrap)]
        [InlineData(stringmiddlenewlines, 4, -1, TextWrapping.NoWrap)]
        [InlineData(stringmiddlenewlines, 4, 150, TextWrapping.Wrap)]
        public void Should_Break_Lines_String_Correctly(string input,
                                                            int linesCount,
                                                            double widthConstraint,
                                                            TextWrapping wrap)
        {
            using (var fmt = Create(input, FontSize, wrap))
            {
                if (widthConstraint != -1)
                {
                    fmt.Constraint = new Size(widthConstraint, 10000);
                }

                var lines = fmt.GetLines().ToArray();
                Assert.Equal(linesCount, lines.Count());
            }
        }

#if AVALONIA_CAIRO
        [Theory(Skip = "TODO: Font scaling currently broken on cairo")]
#else
        [Theory]
#endif
        [InlineData("x", 0, 0, true, false, 0)]
        [InlineData(stringword, 25, 13, true, false, 3)]
        [InlineData(stringword, 28.70, 13.5, true, true, 3)]
        [InlineData(stringword, 30, 13, false, true, 3)]
        [InlineData(stringword, 28, 15, false, true, 3)]
        [InlineData(stringword, 30, 15, false, true, 3)]
        public void Should_HitTestPoint_Correctly(string input,
                                    double x, double y,
                                    bool isInside, bool isTrailing, int pos)
        {
            using (var fmt = Create(input, FontSize))
            {
                var htRes = fmt.HitTestPoint(new Point(x, y));

                Assert.Equal(isInside, htRes.IsInside);
                Assert.Equal(isTrailing, htRes.IsTrailing);
                Assert.Equal(pos, htRes.TextPosition);
            }
        }

#if AVALONIA_CAIRO
        [Theory(Skip = "TODO: Font scaling currently broken on cairo")]
#else
        [Theory]
#endif
        [InlineData("", 0, 0, 0, 0, FontSizeHeight)]
        [InlineData("x", 0, 0, 0, 7.20, FontSizeHeight)]
        [InlineData(stringword, 3, 21.60, 0, 7.20, FontSizeHeight)]
        [InlineData(stringmiddlenewlines, 10, 0, FontSizeHeight, 7.20, FontSizeHeight)]
        [InlineData(stringmiddlenewlines, 20, 0, 2 * FontSizeHeight, 7.20, FontSizeHeight)]
        [InlineData(stringmiddlenewlines, 15, 36.01, FontSizeHeight, 7.20, FontSizeHeight)]
        public void Should_HitTestPosition_Correctly(string input,
                    int index, double x, double y, double width, double height)
        {
            //parse expected
            using (var fmt = Create(input, FontSize))
            {
                var r = fmt.HitTestTextPosition(index);

                Assert.Equal(x, r.X, 2);
                Assert.Equal(y, r.Y, 2);
                Assert.Equal(width, r.Width, 2);
                Assert.Equal(height, r.Height, 2);
            }
        }

#if AVALONIA_CAIRO
        [Theory(Skip = "TODO: Font scaling currently broken on cairo")]
#else
        [Theory]
#endif
        [InlineData("x", 0, 1, "0,0,7.20,13.59")]
        [InlineData(stringword, 0, 4, "0,0,28.80,13.59")]
        [InlineData(stringmiddlenewlines, 10, 10, "0,13.59,57.61,13.59")]
        [InlineData(stringmiddlenewlines, 10, 20, "0,13.59,57.61,13.59;0,27.19,64.81,13.59")]
        [InlineData(stringmiddlenewlines, 10, 15, "0,13.59,57.61,13.59;0,27.19,36.01,13.59")]
        [InlineData(stringmiddlenewlines, 15, 15, "36.01,13.59,21.60,13.59;0,27.19,64.81,13.59")]
        public void Should_HitTestRange_Correctly(string input,
                            int index, int length,
                            string expectedRects)
        {
            //parse expected result
            var rects = expectedRects.Split(';').Select(s =>
            {
                double[] v = s.Split(',')
                .Select(sd => double.Parse(sd, CultureInfo.InvariantCulture)).ToArray();
                return new Rect(v[0], v[1], v[2], v[3]);
            }).ToArray();

            using (var fmt = Create(input, FontSize))
            {
                var htRes = fmt.HitTestTextRange(index, length).ToArray();

                Assert.Equal(rects.Length, htRes.Length);

                for (int i = 0; i < rects.Length; i++)
                {
                    var exr = rects[i];
                    var r = htRes[i];

                    Assert.Equal(exr.X, r.X, 2);
                    Assert.Equal(exr.Y, r.Y, 2);
                    Assert.Equal(exr.Width, r.Width, 2);
                    Assert.Equal(exr.Height, r.Height, 2);
                }
            }
        }
    }
}