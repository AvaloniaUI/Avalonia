#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Automation.Provider;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxTextNavigationTests : ScopedTestBase
    {
        [Fact]
        public void Linear_Scope_Covers_The_Whole_Text()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("Hello world");
            var scope = nav.GetLinearScope(nav.DocumentStart);

            Assert.Equal(11, scope.Length);
            Assert.Equal(0, scope.GetOffset(nav.DocumentStart));
            Assert.Equal(11, scope.GetOffset(nav.DocumentEnd));
            Assert.Equal(0, scope.GetOffset(scope.Range.Start));
            Assert.Equal(11, scope.GetOffset(scope.Range.End));

            // Every position of a TextBox resolves to the same scope.
            Assert.Same(scope, nav.GetLinearScope(nav.DocumentEnd));
        }

        [Fact]
        public void Linear_Scope_Resolves_Offsets_And_Reads_Text()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("Hello world");
            var scope = nav.GetLinearScope(nav.DocumentStart);

            var range = scope.GetRange(6, 5);

            Assert.Equal(6, scope.GetOffset(range.Start));
            Assert.Equal(11, scope.GetOffset(range.End));
            Assert.Equal(LogicalDirection.Forward, range.Start.Gravity);
            Assert.Equal(LogicalDirection.Backward, range.End.Gravity);
            Assert.Equal("world", nav.GetText(range));
            Assert.Equal("world", scope.GetText(6, 5));
        }

        [Fact]
        public void Linear_Scope_Clamps_Offsets()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("abc");
            var scope = nav.GetLinearScope(nav.DocumentStart);

            Assert.Equal(3, scope.GetOffset(scope.GetPosition(100, LogicalDirection.Forward)));
            Assert.Equal(0, scope.GetOffset(scope.GetPosition(-100, LogicalDirection.Backward)));
            Assert.Equal("bc", scope.GetText(1, 100));
            Assert.Equal(string.Empty, scope.GetText(5, 2));
        }

        [Fact]
        public void Linear_Scope_Snaps_Out_Of_A_Surrogate_Pair_In_The_Gravity_Direction()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            // U+1F600 is a surrogate pair at [1, 3).
            var nav = CreateNavigation("a\U0001F600b");
            var scope = nav.GetLinearScope(nav.DocumentStart);

            var forward = scope.GetPosition(2, LogicalDirection.Forward);
            var backward = scope.GetPosition(2, LogicalDirection.Backward);

            Assert.Equal(3, scope.GetOffset(forward));
            Assert.Equal(LogicalDirection.Forward, forward.Gravity);
            Assert.Equal(1, scope.GetOffset(backward));
            Assert.Equal(LogicalDirection.Backward, backward.Gravity);
        }

        [Fact]
        public void Pointers_Order_By_Position_And_Ignore_Gravity_For_Equality()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("abc");
            var scope = nav.GetLinearScope(nav.DocumentStart);

            var one = scope.GetPosition(1, LogicalDirection.Forward);
            var two = scope.GetPosition(2, LogicalDirection.Forward);
            var flipped = nav.WithGravity(one, LogicalDirection.Backward);

            Assert.True(one.CompareTo(two) < 0);
            Assert.True(two.CompareTo(one) > 0);
            Assert.Equal(LogicalDirection.Backward, flipped.Gravity);
            Assert.True(one.Equals(flipped));
            Assert.Equal(0, one.CompareTo(flipped));
        }

        [Fact]
        public void GetRange_Normalizes()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("abc");
            var scope = nav.GetLinearScope(nav.DocumentStart);

            var range = nav.GetRange(
                scope.GetPosition(2, LogicalDirection.Forward),
                scope.GetPosition(1, LogicalDirection.Forward));

            Assert.Equal(1, scope.GetOffset(range.Start));
            Assert.Equal(2, scope.GetOffset(range.End));
            Assert.False(range.IsEmpty);
            Assert.True(nav.GetRange(nav.DocumentStart, nav.DocumentStart).IsEmpty);
        }

        [Fact]
        public void Enclosing_And_Move_By_Word()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("foo bar");

            var word = nav.GetRangeEnclosing(At(nav, 1), TextUnit.Word); // inside "foo"
            Assert.Equal((0, 4), Span(nav, word)); // the word owns its trailing space

            // A single word forward from the start lands on the next word's start.
            Assert.Equal(4, Offset(nav, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 1)));

            Assert.Equal((0, 7), Span(nav, nav.GetRangeEnclosing(At(nav, 1), TextUnit.Document)));
        }

        [Fact]
        public void Word_Owns_Trailing_Whitespace()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("one  two\tthree");

            // A word unit runs to the start of the following word (UIA "word plus
            // trailing whitespace", AT-SPI GRANULARITY_WORD): spaces and tabs after a
            // word belong to it, and expanding inside the gap resolves to that word.
            Assert.Equal((0, 5), Span(nav, nav.GetRangeEnclosing(At(nav, 1), TextUnit.Word)));
            Assert.Equal((0, 5), Span(nav, nav.GetRangeEnclosing(At(nav, 4), TextUnit.Word)));
            Assert.Equal((5, 9), Span(nav, nav.GetRangeEnclosing(At(nav, 6), TextUnit.Word))); // "two" plus its tab

            // One stop per word when stepping by the unit.
            Assert.Equal(5, Offset(nav, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 1)));
            Assert.Equal(9, Offset(nav, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 2)));
            Assert.Equal(14, Offset(nav, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 3)));
        }

        [Fact]
        public void Word_Never_Attaches_Line_Breaks_Or_Leading_Whitespace()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("one \n  two");

            // The space before the break attaches to "one"; the break itself does not.
            Assert.Equal((0, 4), Span(nav, nav.GetRangeEnclosing(At(nav, 3), TextUnit.Word)));
            Assert.Equal((4, 5), Span(nav, nav.GetRangeEnclosing(At(nav, 4), TextUnit.Word)));

            // Whitespace with no word before it on its line stays a gap unit of its own.
            Assert.Equal((5, 7), Span(nav, nav.GetRangeEnclosing(At(nav, 5), TextUnit.Word)));
        }

        [Fact]
        public void Character_Unit_Is_Grapheme_Aware()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("áb");

            // "a" followed by combining acute is a single grapheme spanning two code units.
            Assert.Equal((0, 2), Span(nav, nav.GetRangeEnclosing(nav.DocumentStart, TextUnit.Character)));

            var afterTwo = nav.GetPosition(nav.DocumentStart, TextUnit.Character, 2);
            Assert.Equal(3, Offset(nav, afterTwo));
            Assert.Equal(2, Offset(nav, nav.GetPosition(afterTwo, TextUnit.Character, -1)));
        }

        [Fact]
        public void Word_Unit_Uses_Uax29()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("don't stop");

            // Unicode word segmentation keeps the contraction together; a segmenter that
            // treated the apostrophe as a separator would split it. The word owns its
            // trailing space.
            Assert.Equal((0, 6), Span(nav, nav.GetRangeEnclosing(At(nav, 2), TextUnit.Word)));

            // Word units: 0 | "don't " 6 | "stop" 10.
            Assert.Equal(6, Offset(nav, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 1)));
            Assert.Equal(0, Offset(nav, nav.GetPosition(At(nav, 5), TextUnit.Word, -1)));
        }

        [Fact]
        public void GetRangeEnclosing_Honors_Gravity_At_Boundary()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("foo bar");
            var scope = nav.GetLinearScope(nav.DocumentStart);

            // Offset 4 is the boundary between the "foo " unit [0,4) (the word owns its
            // trailing space) and the "bar" unit [4,7).
            var forward = scope.GetPosition(4, LogicalDirection.Forward);
            var backward = nav.WithGravity(forward, LogicalDirection.Backward);

            Assert.Equal((4, 7), Span(nav, nav.GetRangeEnclosing(forward, TextUnit.Word)));
            Assert.Equal((0, 4), Span(nav, nav.GetRangeEnclosing(backward, TextUnit.Word)));
        }

        [Fact]
        public void TextChanged_Reports_Inserted_Range_And_Bumps_Version()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello" };
            var nav = new TextBoxTextNavigation(textBox);

            TextChange? captured = null;
            nav.TextChanged += (_, c) => captured = c;
            var startVersion = nav.DocumentVersion;

            textBox.Text = "Hello!";

            Assert.NotNull(captured);
            Assert.Equal((5, 6), Span(nav, captured!.Value.Inserted));
            Assert.Equal("!", nav.GetText(captured.Value.Inserted));
            Assert.True(nav.DocumentVersion > startVersion);
        }

        [Fact]
        public void TextChanged_Reports_An_Empty_Range_For_A_Deletion()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello world" };
            var nav = new TextBoxTextNavigation(textBox);

            TextChange? captured = null;
            nav.TextChanged += (_, c) => captured = c;

            textBox.Text = "Hello";

            Assert.NotNull(captured);
            Assert.True(captured!.Value.Inserted.IsEmpty);
            Assert.Equal((5, 5), Span(nav, captured.Value.Inserted));
        }

        [Fact]
        public void Linear_Scope_Reports_The_Replaced_Extent()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello world" };
            var nav = new TextBoxTextNavigation(textBox);
            var scope = nav.GetLinearScope(nav.DocumentStart);

            LinearTextChange? captured = null;
            scope.TextChanged += (_, c) => captured = c;

            textBox.Text = "Hello there";

            Assert.Equal(new LinearTextChange(6, 5, 5), captured);
        }

        [Fact]
        public void GetSelection_Round_Trips_A_Forward_Selection()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello world" };
            var nav = new TextBoxTextNavigation(textBox);

            nav.SetSelection(new DirectedTextRange(At(nav, 6), At(nav, 11)));

            Assert.Equal(6, textBox.SelectionStart);
            Assert.Equal(11, textBox.SelectionEnd);
            Assert.Equal(11, textBox.CaretIndex);

            var selection = nav.GetSelection();
            Assert.False(selection.IsReversed);
            Assert.Equal(6, Offset(nav, selection.Anchor));
            Assert.Equal(11, Offset(nav, selection.Active));
            Assert.Equal("world", nav.GetText(nav.GetRange(selection.Start, selection.End)));
        }

        [Fact]
        public void GetSelection_Keeps_The_Direction_Of_A_Backward_Selection()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello world" };
            var nav = new TextBoxTextNavigation(textBox);

            nav.SetSelection(new DirectedTextRange(At(nav, 11), At(nav, 6)));

            Assert.Equal(11, textBox.SelectionStart);
            Assert.Equal(6, textBox.SelectionEnd);
            Assert.Equal(6, textBox.CaretIndex);

            var selection = nav.GetSelection();
            Assert.True(selection.IsReversed);
            Assert.Equal(6, Offset(nav, selection.Start));
            Assert.Equal(11, Offset(nav, selection.End));
            Assert.Equal(6, Offset(nav, selection.Active));
        }

        [Fact]
        public void GetCaret_Is_An_Empty_Range_At_The_Caret()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello world" };
            var nav = new TextBoxTextNavigation(textBox);

            textBox.CaretIndex = 3;

            var caret = nav.GetCaret();
            Assert.True(caret.IsEmpty);
            Assert.Equal(3, Offset(nav, caret.Start));
        }

        [Fact]
        public void A_Masked_Password_Is_Read_And_Segmented_As_Drawn()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "ab cd", PasswordChar = '*' };
            var nav = new TextBoxTextNavigation(textBox);

            Assert.Equal("*****", nav.GetPlainText(nav.DocumentRange));

            // Units come from the mask, so the space in the password does not show as a word boundary.
            var mask = CreateNavigation("*****");
            Assert.Equal(
                Span(mask, mask.GetRangeEnclosing(mask.DocumentStart, TextUnit.Word)),
                Span(nav, nav.GetRangeEnclosing(nav.DocumentStart, TextUnit.Word)));
            Assert.Equal(
                Offset(mask, mask.GetPosition(mask.DocumentStart, TextUnit.Word, 1)),
                Offset(nav, nav.GetPosition(nav.DocumentStart, TextUnit.Word, 1)));
        }

        [Fact]
        public void A_Revealed_Password_Is_Read_And_Segmented_In_Clear()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "ab cd", PasswordChar = '*', RevealPassword = true };
            var nav = new TextBoxTextNavigation(textBox);

            Assert.Equal("ab cd", nav.GetPlainText(nav.DocumentRange));
            Assert.Equal((0, 3), Span(nav, nav.GetRangeEnclosing(nav.DocumentStart, TextUnit.Word)));
        }

        [Fact]
        public void A_Held_Pointer_Is_A_Snapshot_That_Does_Not_Follow_Edits()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "world" };
            var nav = new TextBoxTextNavigation(textBox);
            var held = At(nav, 0);

            textBox.Text = "hello world";

            Assert.Equal(0, Offset(nav, held));
        }

        [Fact]
        public void GetRange_Keeps_The_Gravity_Of_Its_Endpoints()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var nav = CreateNavigation("abc");
            var scope = nav.GetLinearScope(nav.DocumentStart);

            var range = nav.GetRange(
                scope.GetPosition(2, LogicalDirection.Backward),
                scope.GetPosition(1, LogicalDirection.Forward));

            Assert.Equal(LogicalDirection.Forward, range.Start.Gravity);
            Assert.Equal(LogicalDirection.Backward, range.End.Gravity);
        }

        [Fact]
        public void Tracked_Range_Shifts_With_An_Insertion_Before_It()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "world" };
            var nav = new TextBoxTextNavigation(textBox);
            using var tracked = nav.TrackRange(Inward(nav, 1, 3));

            textBox.Text = "hello world";

            Assert.Equal((7, 10), Span(nav, tracked));
            Assert.Equal("orl", nav.GetText(tracked));
        }

        [Fact]
        public void Tracked_Range_Ignores_An_Insertion_After_It()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "hello" };
            var nav = new TextBoxTextNavigation(textBox);
            using var tracked = nav.TrackRange(Inward(nav, 0, 2));

            textBox.Text = "hello world";

            Assert.Equal((0, 2), Span(nav, tracked));
        }

        [Fact]
        public void Tracked_Range_Grows_With_An_Insertion_Inside_It()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "ac" };
            var nav = new TextBoxTextNavigation(textBox);
            using var tracked = nav.TrackRange(Inward(nav, 0, 2));

            textBox.Text = "abc";

            Assert.Equal((0, 3), Span(nav, tracked));
        }

        [Fact]
        public void Inward_Gravity_Keeps_Typing_At_The_Edges_Outside_The_Range()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "abc" };
            var nav = new TextBoxTextNavigation(textBox);
            using var tracked = nav.TrackRange(Inward(nav, 1, 1));

            textBox.Text = "aXbc";
            Assert.Equal((2, 3), Span(nav, tracked));

            textBox.Text = "aXbYc";
            Assert.Equal((2, 3), Span(nav, tracked));
        }

        [Fact]
        public void Outward_Gravity_Takes_Typing_At_The_Edges_Into_The_Range()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "abc" };
            var nav = new TextBoxTextNavigation(textBox);
            var scope = nav.GetLinearScope(nav.DocumentStart);
            using var tracked = nav.TrackRange(nav.GetRange(
                scope.GetPosition(1, LogicalDirection.Backward),
                scope.GetPosition(2, LogicalDirection.Forward)));

            textBox.Text = "aXbc";
            Assert.Equal((1, 3), Span(nav, tracked));

            textBox.Text = "aXbYc";
            Assert.Equal((1, 4), Span(nav, tracked));
        }

        [Fact]
        public void Tracked_Range_Collapses_When_Its_Content_Is_Removed()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "hello world" };
            var nav = new TextBoxTextNavigation(textBox);
            using var tracked = nav.TrackRange(Inward(nav, 7, 3));

            textBox.Text = "hello d";

            Assert.True(tracked.IsEmpty);
            Assert.Equal((6, 6), Span(nav, tracked));
        }

        [Fact]
        public void Tracked_Range_Covers_The_Replacement_Of_Its_Content()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "a bc d" };
            var nav = new TextBoxTextNavigation(textBox);
            using var tracked = nav.TrackRange(Inward(nav, 2, 2));

            textBox.Text = "a xyz d";

            Assert.Equal("xyz", nav.GetText(tracked));
        }

        [Fact]
        public void An_Empty_Tracked_Range_Never_Inverts()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "ab" };
            var nav = new TextBoxTextNavigation(textBox);

            // A forward start and a backward end at one point would cross on an insertion there.
            using var tracked = nav.TrackRange(Inward(nav, 1, 0));

            textBox.Text = "aXb";

            Assert.True(tracked.IsEmpty);
            Assert.True(tracked.Start.CompareTo(tracked.End) <= 0);
            Assert.Equal((2, 2), Span(nav, tracked));
        }

        [Fact]
        public void Tracked_Range_Is_Current_When_TextChanged_Is_Raised()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "world" };
            var nav = new TextBoxTextNavigation(textBox);
            using var tracked = nav.TrackRange(Inward(nav, 0, 5));

            string? seen = null;
            nav.TextChanged += (_, _) => seen = nav.GetText(tracked);

            textBox.Text = "hello world";

            Assert.Equal("world", seen);
        }

        [Fact]
        public void A_Disposed_Tracked_Range_Stops_Moving()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "world" };
            var nav = new TextBoxTextNavigation(textBox);
            var tracked = nav.TrackRange(Inward(nav, 0, 5));

            tracked.Dispose();
            tracked.Dispose();
            textBox.Text = "hello world";

            Assert.Equal((0, 5), Span(nav, tracked));
        }

        [Fact]
        public void A_Dropped_Tracked_Range_Is_Collected_And_Stops_Being_Tracked()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "world" };
            var nav = new TextBoxTextNavigation(textBox);

            var dropped = TrackAndDrop(nav);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(dropped.TryGetTarget(out var alive));

            textBox.Text = "hello world";

            Assert.Equal(0, nav.TrackedRangeCount);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference<ITrackedTextRange> TrackAndDrop(TextBoxTextNavigation nav)
            => new(nav.TrackRange(nav.DocumentRange));

        [Fact]
        public void Moving_The_Caret_Raises_One_SelectionChanged_With_The_Final_State()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello world", CaretIndex = 2 };
            var nav = new TextBoxTextNavigation(textBox);
            var seen = RecordSelections(nav);

            // CaretIndex moves SelectionStart and SelectionEnd one after the other; the state in between
            // (a selection from 2 to 5) never existed for the user.
            textBox.CaretIndex = 5;

            Assert.Equal(new[] { (5, 5, 5) }, seen);
        }

        [Fact]
        public void Setting_A_Selection_Raises_One_SelectionChanged_With_The_Final_State()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello world" };
            var nav = new TextBoxTextNavigation(textBox);
            var seen = RecordSelections(nav);

            nav.SetSelection(new DirectedTextRange(At(nav, 11), At(nav, 6)));

            Assert.Equal(new[] { (11, 6, 6) }, seen);
        }

        [Fact]
        public void SelectAll_Raises_One_SelectionChanged()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello world", CaretIndex = 3 };
            var nav = new TextBoxTextNavigation(textBox);
            var seen = RecordSelections(nav);

            textBox.SelectAll();

            Assert.Single(seen);
            Assert.Equal((0, 11), (seen[0].Anchor, seen[0].Active));
        }

        [Fact]
        public void Setting_The_Same_Selection_Raises_No_SelectionChanged()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello world" };
            var nav = new TextBoxTextNavigation(textBox);
            nav.SetSelection(new DirectedTextRange(At(nav, 2), At(nav, 4)));
            var seen = RecordSelections(nav);

            nav.SetSelection(new DirectedTextRange(At(nav, 2), At(nav, 4)));

            Assert.Empty(seen);
        }

        [Fact]
        public void PlainTextChanged_Follows_Document_Edits()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "Hello" };
            var nav = new TextBoxTextNavigation(textBox);

            TextChange? captured = null;
            nav.PlainTextChanged += (_, c) => captured = c;

            textBox.Text = "Hello!";

            Assert.NotNull(captured);
            Assert.Equal((5, 6), Span(nav, captured!.Value.Inserted));
        }

        [Fact]
        public void Revealing_And_Masking_A_Password_Raises_PlainTextChanged_For_The_Whole_Text()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "secret", PasswordChar = '*' };
            var nav = new TextBoxTextNavigation(textBox);

            var changes = new List<(int Start, int End, string Text)>();
            nav.PlainTextChanged += (_, c) =>
                changes.Add((Offset(nav, c.Inserted.Start), Offset(nav, c.Inserted.End), nav.GetPlainText(c.Inserted)));

            textBox.RevealPassword = true;
            textBox.RevealPassword = false;
            textBox.PasswordChar = '#';

            Assert.Equal(new[] { (0, 6, "secret"), (0, 6, "******"), (0, 6, "######") }, changes);
        }

        [Fact]
        public void Masking_Changes_That_Draw_The_Same_Text_Raise_No_PlainTextChanged()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "visible" };
            var nav = new TextBoxTextNavigation(textBox);
            var count = 0;
            nav.PlainTextChanged += (_, _) => count++;

            // Without a password character the text is drawn in clear either way.
            textBox.RevealPassword = true;

            var empty = new TextBox { PasswordChar = '*' };
            var emptyNav = new TextBoxTextNavigation(empty);
            emptyNav.PlainTextChanged += (_, _) => count++;
            empty.RevealPassword = true;

            Assert.Equal(0, count);
        }

        [Fact]
        public void The_TextBox_Scope_Stays_Valid_Across_Edits()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var textBox = new TextBox { Text = "one\ntwo" };
            var nav = new TextBoxTextNavigation(textBox);
            var scope = nav.GetLinearScope(nav.DocumentStart);
            var invalidated = false;
            scope.Invalidated += (_, _) => invalidated = true;

            textBox.Text = "onetwo";
            textBox.Text = string.Empty;

            Assert.True(scope.IsValid);
            Assert.False(invalidated);
        }

        [Fact]
        public void Rejects_Foreign_Pointer()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var navA = CreateNavigation("aaa");
            var navB = CreateNavigation("bbb");

            Assert.Throws<ArgumentException>(() => navA.GetRange(navA.DocumentStart, navB.DocumentStart));
            Assert.Throws<ArgumentException>(() => navA.GetLinearScope(navB.DocumentStart));
            Assert.Throws<ArgumentException>(
                () => navA.GetLinearScope(navA.DocumentStart).GetOffset(navB.DocumentStart));
            Assert.Throws<ArgumentException>(() => navA.DocumentStart.CompareTo(navB.DocumentStart));
            Assert.Throws<ArgumentException>(() => navA.TrackRange(navB.DocumentRange));
            Assert.False(navA.DocumentStart.Equals(navB.DocumentStart));
        }

        [Fact]
        public void Rejects_Foreign_Pointer_In_Mutation()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            var a = new TextBox { Text = "aaa" };
            var navA = new TextBoxTextNavigation(a);
            var navB = CreateNavigation("bbb");

            // The mutation path enforces the same provenance rule as the navigation reads.
            Assert.Throws<ArgumentException>(
                () => navA.SetSelection(new DirectedTextRange(navB.DocumentStart, navB.DocumentEnd)));
            Assert.Equal(0, a.SelectionStart);
            Assert.Equal(0, a.SelectionEnd);
        }

        private static IAccessibleText CreateNavigation(string text)
            => new TextBoxTextNavigation(new TextBox { Text = text });

        private static ITextPointer At(ITextNavigation nav, int offset)
            => nav.GetLinearScope(nav.DocumentStart).GetPosition(offset, LogicalDirection.Forward);

        private static int Offset(ITextNavigation nav, ITextPointer position)
            => nav.GetLinearScope(position).GetOffset(position);

        private static List<(int Anchor, int Active, int Caret)> RecordSelections(IAccessibleText nav)
        {
            var seen = new List<(int Anchor, int Active, int Caret)>();
            nav.SelectionChanged += (_, _) =>
            {
                var selection = nav.GetSelection();
                seen.Add((Offset(nav, selection.Anchor), Offset(nav, selection.Active), Offset(nav, nav.GetCaret().Start)));
            };
            return seen;
        }

        private static ITextRange Inward(ITextNavigation nav, int start, int length)
            => nav.GetLinearScope(nav.DocumentStart).GetRange(start, length);

        private static (int Start, int End) Span(ITextNavigation nav, ITextRange range)
            => (Offset(nav, range.Start), Offset(nav, range.End));
    }
}
