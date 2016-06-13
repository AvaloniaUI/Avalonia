// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using Avalonia.Media;
using Xunit;
using Avalonia.Platform;

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
        private const string stringword = "word";
        private const string stringmiddle = "The quick brown fox jumps over the lazy dog";
        private const string stringmiddle2lines = "The quick brown fox\njumps over the lazy dog";
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

        [Theory]
        [InlineData("x", 7.20, 13.59)]
        [InlineData(stringword, 28.80, 13.59)]
        [InlineData(stringmiddle, 309.65, 13.59)]
        [InlineData(stringmiddle2lines, 165.63, 27.19)]
        [InlineData(stringlong, 2160.35, 13.59)]
        [InlineData(stringmiddlenewlines, 72.01, 54.38)]
        public void Should_Measure_String_Correctly(string input, double expWidth, double expHeight)
        {
            using (var fmt = Create(input, FontSize))
            {
                var size = fmt.Measure();

                Assert.Equal(expWidth, size.Width, 2);
                Assert.Equal(expHeight, size.Height, 2);
            }
        }

        [Theory]
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

                var lines = fmt.GetLines();
                Assert.Equal(linesCount, lines.Count());
            }
        }

        [Theory]
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

    }
}