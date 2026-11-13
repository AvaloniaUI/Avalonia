#nullable enable

using System;
using Avalonia.Automation.Provider;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxTextNavigationTests : ScopedTestBase
    {
        [Fact]
        public void Resolves_Offsets_And_Reads_Text()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("Hello world");

            Assert.Equal(0, nav.DocumentStart.Offset);
            Assert.Equal(11, nav.DocumentEnd.Offset);

            // A platform offset becomes a position only by walking from an anchor the navigation
            // produced; there is no factory from a bare integer.
            var range = nav.GetRange(nav.GetPosition(nav.DocumentStart, 6), nav.DocumentEnd);
            var text = nav.GetText(range);

            Assert.Equal("world", text);
            Assert.Equal(range.End.Offset - range.Start.Offset, text.Length); // gapless invariant
        }

        [Fact]
        public void GetPosition_Clamps_And_GetRange_Normalizes()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("abc");

            Assert.Equal(3, nav.GetPosition(nav.DocumentStart, 100).Offset);
            Assert.Equal(0, nav.GetPosition(nav.DocumentEnd, -100).Offset);

            var a = nav.GetPosition(nav.DocumentStart, 2);
            var b = nav.GetPosition(nav.DocumentStart, 1);
            var range = nav.GetRange(a, b); // arguments out of order are normalized

            Assert.Equal(1, range.Start.Offset);
            Assert.Equal(2, range.End.Offset);
            Assert.Equal(-1, nav.GetOffset(a, b));
        }

        [Fact]
        public void Enclosing_And_Move_By_Word()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("foo bar");

            var inWord = nav.GetPosition(nav.DocumentStart, 1); // inside "foo"
            var word = nav.GetRangeEnclosing(inWord, TextUnit.Word);
            Assert.Equal(0, word.Start.Offset);
            Assert.Equal(4, word.End.Offset); // the word owns its trailing space

            // A single word forward from the start lands on the next word's start.
            Assert.Equal(4, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 1).Offset);

            var whole = nav.GetRangeEnclosing(inWord, TextUnit.Document);
            Assert.Equal(0, whole.Start.Offset);
            Assert.Equal(7, whole.End.Offset);
        }

        [Fact]
        public void Word_Owns_Trailing_Whitespace()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("one  two\tthree");

            // A word unit runs to the start of the following word (UIA "word plus
            // trailing whitespace", AT-SPI GRANULARITY_WORD): spaces and tabs after a
            // word belong to it, and expanding inside the gap resolves to that word.
            var first = nav.GetRangeEnclosing(nav.GetPosition(nav.DocumentStart, 1), TextUnit.Word);
            Assert.Equal(0, first.Start.Offset);
            Assert.Equal(5, first.End.Offset);

            var inGap = nav.GetRangeEnclosing(nav.GetPosition(nav.DocumentStart, 4), TextUnit.Word);
            Assert.Equal(0, inGap.Start.Offset);
            Assert.Equal(5, inGap.End.Offset);

            var second = nav.GetRangeEnclosing(nav.GetPosition(nav.DocumentStart, 6), TextUnit.Word);
            Assert.Equal(5, second.Start.Offset);
            Assert.Equal(9, second.End.Offset); // "two" plus its tab

            // One stop per word when stepping by the unit.
            Assert.Equal(5, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 1).Offset);
            Assert.Equal(9, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 2).Offset);
            Assert.Equal(14, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 3).Offset);
        }

        [Fact]
        public void Word_Never_Attaches_Line_Breaks_Or_Leading_Whitespace()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("one \n  two");

            // The space before the break attaches to "one"; the break itself does not.
            var word = nav.GetRangeEnclosing(nav.GetPosition(nav.DocumentStart, 3), TextUnit.Word);
            Assert.Equal(0, word.Start.Offset);
            Assert.Equal(4, word.End.Offset);

            var separator = nav.GetRangeEnclosing(nav.GetPosition(nav.DocumentStart, 4), TextUnit.Word);
            Assert.Equal(4, separator.Start.Offset);
            Assert.Equal(5, separator.End.Offset);

            // Whitespace with no word before it on its line stays a gap unit of its own.
            var leading = nav.GetRangeEnclosing(nav.GetPosition(nav.DocumentStart, 5), TextUnit.Word);
            Assert.Equal(5, leading.Start.Offset);
            Assert.Equal(7, leading.End.Offset);
        }

        [Fact]
        public void Character_Unit_Is_Grapheme_Aware()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("áb");

            // "a" followed by combining acute is a single grapheme spanning two code units.
            var grapheme = nav.GetRangeEnclosing(nav.DocumentStart, TextUnit.Character);
            Assert.Equal(0, grapheme.Start.Offset);
            Assert.Equal(2, grapheme.End.Offset);

            var afterTwo = nav.GetPosition(nav.DocumentStart, TextUnit.Character, 2);
            Assert.Equal(3, afterTwo.Offset);
            Assert.Equal(2, nav.GetPosition(afterTwo, TextUnit.Character, -1).Offset);
        }

        [Fact]
        public void Word_Unit_Uses_Uax29()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("don't stop");

            // Unicode word segmentation keeps the contraction together; a segmenter that
            // treated the apostrophe as a separator would split it. The word owns its
            // trailing space.
            var word = nav.GetRangeEnclosing(nav.GetPosition(nav.DocumentStart, 2), TextUnit.Word);
            Assert.Equal(0, word.Start.Offset);
            Assert.Equal(6, word.End.Offset);

            // Word units: 0 | "don't " 6 | "stop" 10.
            Assert.Equal(6, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 1).Offset);
            Assert.Equal(0, nav.GetPosition(nav.GetPosition(nav.DocumentStart, 5), TextUnit.Word, -1).Offset);
        }

        [Fact]
        public void GetRangeEnclosing_Honors_Gravity_At_Boundary()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("foo bar");

            // Offset 4 is the boundary between the "foo " unit [0,4) (the word owns its
            // trailing space) and the "bar" unit [4,7).
            var forward = nav.PointerAt(4, LogicalDirection.Forward);
            var backward = nav.PointerAt(4, LogicalDirection.Backward);

            var following = nav.GetRangeEnclosing(forward, TextUnit.Word);
            Assert.Equal(4, following.Start.Offset);
            Assert.Equal(7, following.End.Offset);

            var preceding = nav.GetRangeEnclosing(backward, TextUnit.Word);
            Assert.Equal(0, preceding.Start.Offset);
            Assert.Equal(4, preceding.End.Offset);
        }

        [Fact]
        public void TextChanged_Reports_Delta_And_Bumps_Version()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello" };
            var nav = new TextBoxTextNavigation(textBox);

            TextChange? captured = null;
            nav.TextChanged += (_, c) => captured = c;
            var startVersion = nav.DocumentVersion;

            textBox.Text = "Hello!";

            Assert.NotNull(captured);
            Assert.Equal(5, captured!.Value.Position.Offset);
            Assert.Equal(0, captured.Value.OldLength);
            Assert.Equal(1, captured.Value.NewLength);
            Assert.True(nav.DocumentVersion > startVersion);

            // The inserted text is read on demand from the change position.
            var inserted = nav.GetRange(
                captured.Value.Position,
                nav.GetPosition(captured.Value.Position, captured.Value.NewLength));
            Assert.Equal("!", nav.GetText(inserted));
        }

        [Fact]
        public void GetSelection_Round_Trips_The_Control_Selection()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello world" };
            var nav = new TextBoxTextNavigation(textBox);

            nav.SetSelection(nav.GetRange(
                nav.GetPosition(nav.DocumentStart, 6),
                nav.GetPosition(nav.DocumentStart, 11)));

            Assert.Equal(6, textBox.SelectionStart);
            Assert.Equal(11, textBox.SelectionEnd);

            var selection = nav.GetSelection();
            Assert.Equal(6, selection.Start.Offset);
            Assert.Equal(11, selection.End.Offset);
            Assert.Equal("world", nav.GetText(selection));
        }

        [Fact]
        public void Rejects_Foreign_Pointer()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var navA = CreateNavigation("aaa");
            var navB = CreateNavigation("bbb");

            Assert.Throws<ArgumentException>(() => navA.GetOffset(navA.DocumentStart, navB.DocumentStart));
        }

        [Fact]
        public void Rejects_Foreign_Pointer_In_Mutation()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var a = new TextBox { Text = "aaa" };
            var navA = new TextBoxTextNavigation(a);
            var navB = CreateNavigation("bbb");

            // The mutation path enforces the same provenance rule as the navigation reads.
            Assert.Throws<ArgumentException>(() => navA.SetSelection(navB.DocumentRange));
            Assert.Equal(0, a.SelectionStart);
            Assert.Equal(0, a.SelectionEnd);
        }

        private static IAccessibleText CreateNavigation(string text)
            => new TextBoxTextNavigation(new TextBox { Text = text });
    }
}
