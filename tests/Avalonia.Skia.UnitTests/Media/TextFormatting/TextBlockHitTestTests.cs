using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextBlockHitTestTests
    {
        public static IEnumerable<object[]> TextCases()
        {
            foreach (var text in new[] { "abc רשימות", "רשימות", "abc العربية", "العربية", "רשימות abc", "abc" })
            {
                foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
                {
                    foreach (var inlines in new[] { false, true })
                    {
                        yield return [text, direction, inlines];
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(TextCases))]
        public void Should_Preserve_Hit_Test_Bounds_With_EndOfParagraph(
            string text, FlowDirection direction, bool inlines)
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(
                renderInterface: new PlatformRenderInterface(null),
                fontManagerImpl: new CustomFontManagerImpl()));

            var fontFamily = new FontFamily(
                "resm:Avalonia.Skia.UnitTests.Fonts.DejaVuSans.ttf?assembly=Avalonia.Skia.UnitTests#DejaVu Sans");
            var textBlock = new TextBlock
            {
                FontFamily = fontFamily,
                FontSize = 24,
                FlowDirection = direction
            };

            if (inlines)
            {
                textBlock.Inlines!.Add(new Run(text));
            }
            else
            {
                textBlock.Text = text;
            }

            textBlock.Measure(Size.Infinity);

            using var expected = new TextLayout(text, new Typeface(fontFamily), 24, Brushes.Black,
                flowDirection: direction);
            var actual = textBlock.TextLayout;

            for (var start = 0; start < text.Length; start++)
            {
                for (var length = 1; length <= text.Length - start; length++)
                {
                    var expectedBounds = expected.HitTestTextRange(start, length).ToArray();
                    var actualBounds = actual.HitTestTextRange(start, length).ToArray();

                    Assert.Equal(expectedBounds.Length, actualBounds.Length);

                    for (var i = 0; i < expectedBounds.Length; i++)
                    {
                        Assert.Equal(expectedBounds[i].Left, actualBounds[i].Left, 5);
                        Assert.Equal(expectedBounds[i].Right, actualBounds[i].Right, 5);
                    }
                }

                foreach (var hit in new[] { new CharacterHit(start), new CharacterHit(start, 1) })
                {
                    Assert.Equal(expected.TextLines[0].GetDistanceFromCharacterHit(hit),
                        actual.TextLines[0].GetDistanceFromCharacterHit(hit), 5);
                }
            }
        }
    }
}
