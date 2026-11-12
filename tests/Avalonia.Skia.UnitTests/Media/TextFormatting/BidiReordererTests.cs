using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class BidiReordererTests
    {
        // Each case lists the bidi level of every run in logical order and the expected
        // logical index of the run at each visual position.
        public static IEnumerable<object[]> LevelCases()
        {
            // Single level-0 run, nothing to reorder.
            yield return [FlowDirection.LeftToRight, new sbyte[] { 0 }, new[] { 0 }];
            // Two LTR runs inside an RTL paragraph keep their relative order and sit left of the RTL run (3-cycle).
            yield return [FlowDirection.RightToLeft, new sbyte[] { 1, 2, 2 }, new[] { 1, 2, 0 }];
            yield return [FlowDirection.RightToLeft, new sbyte[] { 2, 2, 1 }, new[] { 2, 0, 1 }];
            yield return [FlowDirection.RightToLeft, new sbyte[] { 1, 2, 2, 1 }, new[] { 3, 1, 2, 0 }];
            yield return [FlowDirection.RightToLeft, new sbyte[] { 2, 2, 2, 1 }, new[] { 3, 0, 1, 2 }];
            // LTR runs mixed with RTL runs in an LTR paragraph.
            yield return [FlowDirection.LeftToRight, new sbyte[] { 0, 1, 1, 0 }, new[] { 0, 2, 1, 3 }];
            yield return [FlowDirection.LeftToRight, new sbyte[] { 0, 1, 2, 1, 0 }, new[] { 0, 3, 2, 1, 4 }];
            yield return [FlowDirection.LeftToRight, new sbyte[] { 1, 1, 0, 0 }, new[] { 1, 0, 2, 3 }];
        }

        [Theory]
        [MemberData(nameof(LevelCases))]
        public void Should_Map_Logical_Runs_To_Visual_RunIndex(FlowDirection flowDirection, sbyte[] levels, int[] visualToLogical)
        {
            using (Start())
            {
                var properties = new GenericTextRunProperties(Typeface.Default);

                var logicalRuns = levels
                    .Select((level, i) => (TextRun)new ShapedTextRun(
                        TextShaper.Current.ShapeText(
                            new string((char)('a' + i), i + 1),
                            new TextShaperOptions(Typeface.Default.GlyphTypeface, 10, level, CultureInfo.CurrentCulture)),
                        properties))
                    .ToArray();

                var textRuns = logicalRuns.ToArray();

                var indexedRuns = BidiReorderer.Instance.BidiReorder(textRuns, flowDirection, 5);

                Assert.Equal(levels.Length, indexedRuns.Length);

                // textRuns is rewritten into visual order.
                for (var visual = 0; visual < visualToLogical.Length; visual++)
                {
                    Assert.Same(logicalRuns[visualToLogical[visual]], textRuns[visual]);
                }

                // IndexedTextRun stays in logical order and RunIndex points at the run's visual position.
                var expectedSourceIndex = 5;

                for (var logical = 0; logical < indexedRuns.Length; logical++)
                {
                    var indexed = indexedRuns[logical];

                    Assert.Same(logicalRuns[logical], indexed.TextRun);
                    Assert.Equal(expectedSourceIndex, indexed.TextSourceCharacterIndex);
                    Assert.Equal(System.Array.IndexOf(visualToLogical, logical), indexed.RunIndex);
                    Assert.Same(indexed.TextRun, textRuns[indexed.RunIndex]);

                    expectedSourceIndex += logicalRuns[logical].Length;
                }
            }
        }

        private static System.IDisposable Start()
        {
            return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null),
                    fontManagerImpl: new CustomFontManagerImpl()));
        }
    }
}
