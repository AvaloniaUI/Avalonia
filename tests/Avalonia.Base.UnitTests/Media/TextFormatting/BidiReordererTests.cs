using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

public class BidiReordererTests : ScopedTestBase
{
    [Fact]
    public void Logical_Entries_Point_To_Their_Visual_Run_After_Nested_Reordering()
    {
        using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
        {
            var properties = new GenericTextRunProperties(Typeface.Default);
            // Reversing the level-2 group, then the level-1 paragraph produces a
            // non-self-inverse permutation: [0, 1, 2] -> [2, 0, 1].
            using var first = CreateRun("ab", 2);
            using var second = CreateRun("cde", 2);
            using var third = CreateRun("אבגד", 1);
            TextRun[] runs = [first, second, third];

            var indexed = BidiReorderer.Instance.BidiReorder(runs, FlowDirection.RightToLeft, 7);

            Assert.Equal(new TextRun[] { third, first, second }, runs);
            Assert.Equal(new[] { 7, 9, 12 }, indexed.Select(r => r.TextSourceCharacterIndex));
            Assert.Equal(new[] { 1, 2, 0 }, indexed.Select(r => r.RunIndex));
            foreach (var entry in indexed)
                Assert.Same(entry.TextRun, runs[entry.RunIndex]);

            ShapedTextRun CreateRun(string text, sbyte level) => new(
                new ShapedBuffer(text.AsMemory(), 0, Typeface.Default.GlyphTypeface, 12, level), properties);
        }
    }
}
