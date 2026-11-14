#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests;

// HomeEndBehavior is process-wide state. Keep all policy overrides in a
// non-parallel xUnit collection, and restore the value after every test.
[CollectionDefinition("TextBox Home/End handling", DisableParallelization = true)]
public class TextBoxTests_HomeEndCollection { }

[Collection("TextBox Home/End handling")]
public class TextBoxTests_HomeEnd : ScopedTestBase
{
    private const string FourLines = "aaaa\nbbbb\ncccc\ndddd";

    [Fact]
    public void Apple_Gestures_Use_Command_Arrows_For_Lines_And_UpDown_For_Document()
    {
        var map = PlatformHotkeyConfiguration.Apple;
        AssertGesture(map.MoveCursorToTheStartOfLine, Key.Left, KeyModifiers.Meta);
        AssertGesture(map.MoveCursorToTheEndOfLine, Key.Right, KeyModifiers.Meta);
        AssertGesture(map.MoveCursorToTheStartOfLineWithSelection, Key.Left, KeyModifiers.Meta | KeyModifiers.Shift);
        AssertGesture(map.MoveCursorToTheEndOfLineWithSelection, Key.Right, KeyModifiers.Meta | KeyModifiers.Shift);
        AssertGesture(map.MoveCursorToTheStartOfDocument, Key.Up, KeyModifiers.Meta);
        AssertGesture(map.MoveCursorToTheEndOfDocument, Key.Down, KeyModifiers.Meta);
        AssertGesture(map.MoveCursorToTheStartOfDocumentWithSelection, Key.Up, KeyModifiers.Meta | KeyModifiers.Shift);
        AssertGesture(map.MoveCursorToTheEndOfDocumentWithSelection, Key.Down, KeyModifiers.Meta | KeyModifiers.Shift);
        Assert.DoesNotContain(map.MoveCursorToTheStartOfLine, x => x.Key == Key.Home);
        Assert.DoesNotContain(map.MoveCursorToTheEndOfLine, x => x.Key == Key.End);
        Assert.DoesNotContain(map.MoveCursorToTheStartOfDocument, x => x.Key == Key.Home);
        Assert.DoesNotContain(map.MoveCursorToTheEndOfDocument, x => x.Key == Key.End);
    }

    [Fact]
    public void Apple_HomeEnd_Gestures_Are_Scrolling_Or_Document_Selection()
    {
        var map = PlatformHotkeyConfiguration.Apple;
        AssertGesture(map.ScrollToTheStartOfDocument, Key.Home, KeyModifiers.None);
        AssertGesture(map.ScrollToTheEndOfDocument, Key.End, KeyModifiers.None);
        AssertGesture(map.ExtendSelectionToTheStartOfDocument, Key.Home, KeyModifiers.Shift);
        AssertGesture(map.ExtendSelectionToTheEndOfDocument, Key.End, KeyModifiers.Shift);
        Assert.DoesNotContain(map.MoveCursorToTheStartOfLineWithSelection, x => x.Key == Key.Home);
        Assert.DoesNotContain(map.MoveCursorToTheEndOfLineWithSelection, x => x.Key == Key.End);
    }

    [Fact]
    public void Default_Gestures_Preserve_HomeEnd_And_Control_HomeEnd()
    {
        var map = new PlatformHotkeyConfiguration();
        AssertGesture(map.MoveCursorToTheStartOfLine, Key.Home, KeyModifiers.None);
        AssertGesture(map.MoveCursorToTheEndOfLine, Key.End, KeyModifiers.None);
        AssertGesture(map.MoveCursorToTheStartOfDocument, Key.Home, KeyModifiers.Control);
        AssertGesture(map.MoveCursorToTheEndOfDocument, Key.End, KeyModifiers.Control);
        AssertGesture(map.MoveCursorToTheStartOfLineWithSelection, Key.Home, KeyModifiers.Shift);
        AssertGesture(map.MoveCursorToTheEndOfLineWithSelection, Key.End, KeyModifiers.Shift);
        Assert.Empty(map.ScrollToTheStartOfDocument);
        Assert.Empty(map.ScrollToTheEndOfDocument);
    }

    [Theory]
    [InlineData(Key.Left, 5)]
    [InlineData(Key.Right, 9)]
    [InlineData(Key.Up, 0)]
    [InlineData(Key.Down, 19)]
    public void Apple_Command_Navigation_Uses_Correct_Line_Or_Document_Edge(Key key, int expected)
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var (box, _, _) = CreateBox(FourLines);
            box.CaretIndex = 7;
            Assert.True(Press(box, key, KeyModifiers.Meta).Handled);
            Assert.Equal(expected, box.CaretIndex);
            Assert.Equal(expected, box.SelectionStart);
            Assert.Equal(expected, box.SelectionEnd);
        }
    }

    [Fact]
    public void Apple_HomeEnd_Scroll_Without_Modifying_Selection_Caret_Or_Text()
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var text = string.Join("\n", Enumerable.Range(0, 70).Select(i => $"line {i:00}"));
            var (box, _, _) = CreateBox(text, width: 140, height: 50);
            var viewer = box.FindDescendantOfType<ScrollViewer>();
            Assert.NotNull(viewer);
            Assert.True(viewer.Extent.Height > viewer.Viewport.Height);
            Select(box, caret: 8, start: 2, end: 18);

            Assert.True(Press(box, Key.End).Handled);
            Assert.True(viewer.Offset.Y > 0);
            Assert.Equal(8, box.CaretIndex);
            Assert.Equal(2, box.SelectionStart);
            Assert.Equal(18, box.SelectionEnd);

            Assert.True(Press(box, Key.Home).Handled);
            Assert.Equal(0, viewer.Offset.Y);
            Assert.Equal(8, box.CaretIndex);
            Assert.Equal(2, box.SelectionStart);
            Assert.Equal(18, box.SelectionEnd);
            Assert.Equal(text, box.Text);
        }
    }

    [Theory]
    [InlineData(Key.Home, 5)]
    [InlineData(Key.End, 9)]
    public void Caret_Relative_Mode_Ignores_Other_Selection_Boundary(Key key, int expected)
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToCaret);
        using (UnitTestApplication.Start(Services))
        {
            var (box, _, _) = CreateBox(FourLines);
            Select(box, caret: 7, start: 2, end: 16);
            Assert.True(Press(box, key).Handled);
            Assert.Equal(expected, box.CaretIndex);
            Assert.Equal(expected, box.SelectionStart);
            Assert.Equal(expected, box.SelectionEnd);
        }
    }

    [Theory]
    [InlineData(Key.Left, false, false, 0)]
    [InlineData(Key.Left, false, true, 0)]
    [InlineData(Key.Left, true, false, 0)]
    [InlineData(Key.Left, true, true, 0)]
    [InlineData(Key.Right, false, false, 19)]
    [InlineData(Key.Right, false, true, 19)]
    [InlineData(Key.Right, true, false, 19)]
    [InlineData(Key.Right, true, true, 19)]
    public void Apple_Unshifted_Command_Arrow_Collapses_From_Earliest_Or_Latest_Selected_Line(
        Key arrow, bool reverse, bool caretAtHighBoundary, int expected)
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var (box, _, _) = CreateBox(FourLines);
            Select(box, caretAtHighBoundary ? 16 : 2, reverse ? 16 : 2, reverse ? 2 : 16);
            Assert.True(Press(box, arrow, KeyModifiers.Meta).Handled);
            Assert.Equal(expected, box.CaretIndex);
            Assert.Equal(expected, box.SelectionStart);
            Assert.Equal(expected, box.SelectionEnd);
        }
    }

    // Hard newlines are barriers for selection contraction. Repeating the
    // gesture at that line edge must not consume the newline or cross it.
    [Theory]
    [InlineData(Key.Left, 16, 15)]
    [InlineData(Key.Right, 2, 4)]
    public void Apple_Shift_Command_Arrow_Contracts_Only_As_Far_As_Hard_Newline(
        Key arrow, int caret, int expectedEnd)
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var (box, _, presenter) = CreateBox(FourLines);
            Select(box, caret, 2, 16);

            Assert.True(Press(box, arrow, KeyModifiers.Meta | KeyModifiers.Shift).Handled);
            Assert.Equal(2, box.SelectionStart);
            Assert.Equal(expectedEnd, box.SelectionEnd);
            Assert.Equal(expectedEnd, presenter.CaretIndex);

            Assert.True(Press(box, arrow, KeyModifiers.Meta | KeyModifiers.Shift).Handled);
            Assert.Equal(2, box.SelectionStart);
            Assert.Equal(expectedEnd, box.SelectionEnd);
            Assert.Equal(expectedEnd, presenter.CaretIndex);
        }
    }

    [Theory]
    [InlineData(Key.Left, 8, 16, 5, 16)]
    [InlineData(Key.Right, 2, 11, 2, 14)]
    public void Apple_Shift_Command_Arrow_Expands_Selection_When_Moving_Outside_It(
        Key arrow, int first, int last, int expectedMin, int expectedMax)
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var (box, _, presenter) = CreateBox(FourLines);
            Select(box, arrow == Key.Left ? first : last, first, last);
            Assert.True(Press(box, arrow, KeyModifiers.Meta | KeyModifiers.Shift).Handled);
            Assert.Equal(expectedMin, Math.Min(box.SelectionStart, box.SelectionEnd));
            Assert.Equal(expectedMax, Math.Max(box.SelectionStart, box.SelectionEnd));
            Assert.Equal(arrow == Key.Left ? expectedMin : expectedMax, presenter.CaretIndex);
        }
    }

    [Theory]
    [InlineData(Key.Up, 0)]
    [InlineData(Key.Down, 19)]
    public void Apple_Shift_Command_UpDown_Extends_Selection_To_Document_Boundary(
        Key arrow, int expectedEdge)
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var (box, _, presenter) = CreateBox(FourLines);
            box.CaretIndex = 7;
            Assert.True(Press(box, arrow, KeyModifiers.Meta | KeyModifiers.Shift).Handled);
            Assert.Equal(Math.Min(7, expectedEdge), Math.Min(box.SelectionStart, box.SelectionEnd));
            Assert.Equal(Math.Max(7, expectedEdge), Math.Max(box.SelectionStart, box.SelectionEnd));
            Assert.Equal(expectedEdge, presenter.CaretIndex);
        }
    }

    [Theory]
    [InlineData(Key.Home, 0, 16)]
    [InlineData(Key.End, 2, 19)]
    public void Apple_Shift_HomeEnd_Extends_Reverse_Selection_To_Document_Boundary(
        Key key, int expectedMin, int expectedMax)
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var (box, _, presenter) = CreateBox(FourLines);
            Select(box, caret: 2, start: 16, end: 2);
            Assert.True(Press(box, key, KeyModifiers.Shift).Handled);
            Assert.Equal(expectedMin, Math.Min(box.SelectionStart, box.SelectionEnd));
            Assert.Equal(expectedMax, Math.Max(box.SelectionStart, box.SelectionEnd));
            Assert.Equal(key == Key.Home ? 0 : box.Text!.Length, presenter.CaretIndex);
        }
    }

    [Theory]
    [InlineData(Key.Home, 5)]
    [InlineData(Key.End, 9)]
    public void Caret_Relative_Shift_HomeEnd_Extends_From_Original_Caret(Key key, int expectedEnd)
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToCaret);
        using (UnitTestApplication.Start(Services))
        {
            var (box, _, presenter) = CreateBox(FourLines);
            box.CaretIndex = 7;
            Assert.True(Press(box, key, KeyModifiers.Shift).Handled);
            Assert.Equal(7, box.SelectionStart);
            Assert.Equal(expectedEnd, box.SelectionEnd);
            Assert.Equal(expectedEnd, presenter.CaretIndex);
        }
    }

    [Fact]
    public void Apple_Mouse_Selection_Is_Normalized_After_Backwards_Drag()
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            var (box, _, presenter) = CreateBox("ABCDEFGHIJKLMNOPQRSTUVWXYZ", width: 235, height: 50);
            var mouse = new MouseTestHelper();
            mouse.Down(box, MouseButton.Left, new Point(130, 10));
            mouse.Move(box, new Point(2, 10));
            Assert.True(box.SelectionStart > box.SelectionEnd,
                "Fixture must form a reverse selection before the release.");
            mouse.Up(box, MouseButton.Left, new Point(2, 10));
            Assert.True(box.SelectionStart < box.SelectionEnd);
            Assert.Equal(box.SelectionEnd, presenter.CaretIndex);
            Assert.Equal(box.SelectionEnd, box.CaretIndex);
        }
    }

    [Fact]
    public void Caret_Relative_Mouse_Selection_Preserves_Backwards_Drag_Direction()
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToCaret);
        using (UnitTestApplication.Start(Services))
        {
            var (box, _, _) = CreateBox("ABCDEFGHIJKLMNOPQRSTUVWXYZ", width: 235, height: 50);
            var mouse = new MouseTestHelper();
            mouse.Down(box, MouseButton.Left, new Point(130, 10));
            mouse.Move(box, new Point(2, 10));
            Assert.True(box.SelectionStart > box.SelectionEnd);
            mouse.Up(box, MouseButton.Left, new Point(2, 10));
            Assert.True(box.SelectionStart > box.SelectionEnd);
        }
    }

    [Fact]
    public void Apple_Shift_Command_Left_Contracts_Across_Soft_Wrap()
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var (box, _, presenter) = CreateBox(
                "alpha bravo charlie delta echo foxtrot golf hotel india juliet kilo lima",
                width: 90, height: 260, wrapping: TextWrapping.Wrap);

            var lines = presenter.TextLayout.TextLines;
            var candidates = Enumerable.Range(1, lines.Count - 1)
                .Where(i => lines[i - 1].TextLineBreak?.IsSplit == true &&
                    lines[i - 1].Length - lines[i - 1].TrailingWhitespaceLength >= 3 &&
                    lines[i - 1].TrailingWhitespaceLength > 0 &&
                    lines[i].Length >= 4).ToArray();
            Assert.NotEmpty(candidates);

            var previous = lines[candidates[0] - 1];
            var current = lines[candidates[0]];
            var anchor = previous.FirstTextSourceIndex + 1;
            var expectedEnd = previous.FirstTextSourceIndex +
                previous.Length - previous.TrailingWhitespaceLength;

            Select(box, current.FirstTextSourceIndex + 2, anchor,
                current.FirstTextSourceIndex + 3);

            Assert.True(Press(box, Key.Left, KeyModifiers.Meta | KeyModifiers.Shift).Handled);
            Assert.Equal(anchor, box.SelectionStart);
            Assert.Equal(expectedEnd, box.SelectionEnd);
            Assert.Equal(expectedEnd, presenter.CaretIndex);
        }
    }

    [Fact]
    public void Apple_Shift_Command_Right_Contracts_Across_Soft_Wrap()
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var (box, _, presenter) = CreateBox(
                "alpha bravo charlie delta echo foxtrot golf hotel india juliet kilo lima",
                width: 90, height: 260, wrapping: TextWrapping.Wrap);

            var lines = presenter.TextLayout.TextLines;
            var candidates = Enumerable.Range(0, lines.Count - 1)
                .Where(i => lines[i].TextLineBreak?.IsSplit == true &&
                    lines[i].Length - lines[i].TrailingWhitespaceLength >= 3 &&
                    lines[i].TrailingWhitespaceLength > 0 &&
                    lines[i + 1].Length >= 3).ToArray();
            Assert.NotEmpty(candidates);

            var current = lines[candidates[0]];
            var next = lines[candidates[0] + 1];
            var anchor = current.FirstTextSourceIndex;
            var expectedEnd = next.FirstTextSourceIndex;
            Assert.Equal(current.FirstTextSourceIndex + current.Length, expectedEnd);

            Select(box, current.FirstTextSourceIndex + 1, anchor, expectedEnd + 2);

            Assert.True(Press(box, Key.Right, KeyModifiers.Meta | KeyModifiers.Shift).Handled);
            Assert.Equal(anchor, box.SelectionStart);
            Assert.Equal(expectedEnd, box.SelectionEnd);
            Assert.Equal(expectedEnd, presenter.CaretIndex);
        }
    }

    [Fact]
    public void Apple_Shift_Command_Left_At_Soft_Wrap_Start_Can_Jump_To_Previous_Line_Start()
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var (box, _, presenter) = CreateBox(
                "alpha bravo charlie delta echo foxtrot golf hotel india juliet kilo lima",
                width: 90, height: 260, wrapping: TextWrapping.Wrap);

            var lines = presenter.TextLayout.TextLines;
            var candidates = Enumerable.Range(1, lines.Count - 1)
                .Where(i => lines[i - 1].TextLineBreak?.IsSplit == true &&
                    lines[i - 1].Length >= 4 && lines[i].Length >= 3).ToArray();
            Assert.NotEmpty(candidates);

            var previous = lines[candidates[0] - 1];
            var current = lines[candidates[0]];
            var anchor = previous.FirstTextSourceIndex + 1;
            Select(box, current.FirstTextSourceIndex, anchor,
                current.FirstTextSourceIndex + 1);

            Assert.True(Press(box, Key.Left, KeyModifiers.Meta | KeyModifiers.Shift).Handled);
            Assert.Equal(anchor, box.SelectionStart);
            Assert.Equal(previous.FirstTextSourceIndex, box.SelectionEnd);
            Assert.Equal(previous.FirstTextSourceIndex, presenter.CaretIndex);
        }
    }

    [Theory]
    [InlineData(Key.Left)]
    [InlineData(Key.Right)]
    public void Apple_Shift_Command_Arrow_Expands_Only_To_Current_Soft_Wrapped_Line_Edge(
        Key arrow)
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var (box, _, presenter) = CreateBox(
                "alpha bravo charlie delta echo foxtrot golf hotel india juliet kilo lima",
                width: 90, height: 260, wrapping: TextWrapping.Wrap);

            var lines = presenter.TextLayout.TextLines;
            var candidates = Enumerable.Range(1, lines.Count - 1)
                .Where(i => lines[i - 1].TextLineBreak?.IsSplit == true &&
                    lines[i].TextLineBreak?.IsSplit == true &&
                    lines[i].Length - lines[i].TrailingWhitespaceLength >= 5).ToArray();
            Assert.NotEmpty(candidates);
            var current = lines[candidates[0]];

            var first = current.FirstTextSourceIndex + 1;
            var last = current.FirstTextSourceIndex + 2;
            var expected = arrow == Key.Left ? current.FirstTextSourceIndex
                : current.FirstTextSourceIndex + current.Length -
                  current.TrailingWhitespaceLength;

            Select(box, arrow == Key.Left ? first : last, first, last);

            Assert.True(Press(box, arrow, KeyModifiers.Meta | KeyModifiers.Shift).Handled);
            Assert.Equal(arrow == Key.Left ? expected : first,
                Math.Min(box.SelectionStart, box.SelectionEnd));
            Assert.Equal(arrow == Key.Left ? last : expected,
                Math.Max(box.SelectionStart, box.SelectionEnd));
            Assert.Equal(expected, presenter.CaretIndex);
        }
    }

    [Fact]
    public void Apple_Shift_Command_Left_Stops_At_Hard_Break_After_A_Soft_Wrapped_Paragraph()
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToSelectionBoundaries);
        using (UnitTestApplication.Start(Services))
        {
            AppleKeymap();
            var (box, _, presenter) = CreateBox(
                "alpha bravo charlie delta echo foxtrot golf hotel\n" +
                "india juliet kilo lima mike november oscar papa",
                width: 90, height: 290, wrapping: TextWrapping.Wrap);

            var lines = presenter.TextLayout.TextLines;
            var candidates = Enumerable.Range(1, lines.Count - 1)
                .Where(i => lines[i - 1].NewLineLength > 0 &&
                    lines[i].Length >= 3).ToArray();
            Assert.NotEmpty(candidates);
            var current = lines[candidates[0]];
            var anchor = Math.Max(0, current.FirstTextSourceIndex - 4);
            var farEnd = current.FirstTextSourceIndex + 2;
            Select(box, farEnd, anchor, farEnd);

            Assert.True(Press(box, Key.Left, KeyModifiers.Meta | KeyModifiers.Shift).Handled);
            Assert.Equal(anchor, box.SelectionStart);
            Assert.Equal(current.FirstTextSourceIndex, box.SelectionEnd);

            Assert.True(Press(box, Key.Left, KeyModifiers.Meta | KeyModifiers.Shift).Handled);
            Assert.Equal(current.FirstTextSourceIndex, box.SelectionEnd);
            Assert.Equal(current.FirstTextSourceIndex, presenter.CaretIndex);
        }
    }

    [Fact]
    public void End_Before_Whitespace_Soft_Wrap_And_Left_Remain_On_Preceding_Visual_Line()
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToCaret);
        using (UnitTestApplication.Start(Services))
        {
            var (box, _, presenter) = CreateBox(
                "alpha bravo charlie delta echo foxtrot golf hotel india juliet kilo lima",
                width: 76, height: 260, wrapping: TextWrapping.Wrap);
            var lines = presenter.TextLayout.TextLines;
            var index = Enumerable.Range(1, lines.Count - 1).FirstOrDefault(i =>
                lines[i].TextLineBreak?.IsSplit == true && lines[i].TrailingWhitespaceLength > 0);
            Assert.True(index > 0, "Fixture must produce a later line ending with wrap whitespace.");
            var line = lines[index];
            Assert.Equal(0, line.NewLineLength);
            var end = line.FirstTextSourceIndex + line.Length - line.TrailingWhitespaceLength;
            box.CaretIndex = line.FirstTextSourceIndex + 1;
            Assert.True(Press(box, Key.End).Handled);
            Assert.Equal(end, box.CaretIndex);
            var expectedY = lines.Take(index).Sum(x => x.Height);
            Assert.InRange(presenter.CursorRectangle.Y, expectedY - 0.5, expectedY + 0.5);

            Assert.True(Press(box, Key.Left).Handled);
            Assert.Equal(end - 1, box.CaretIndex);
            Assert.InRange(presenter.CursorRectangle.Y, expectedY - 0.5, expectedY + 0.5);
        }
    }

    [Fact]
    public void End_At_Forced_Midword_Wrap_Preserves_Trailing_Caret_Affinity()
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToCaret);
        using (UnitTestApplication.Start(Services))
        {
            var (box, _, presenter) = CreateBox(new string('W', 90),
                width: 64, height: 260, wrapping: TextWrapping.Wrap);
            var lines = presenter.TextLayout.TextLines;
            Assert.True(lines.Count >= 3);
            var line = lines[1];
            Assert.True(line.TextLineBreak?.IsSplit == true);
            Assert.Equal(0, line.TrailingWhitespaceLength);
            var end = line.FirstTextSourceIndex + line.Length;

            box.CaretIndex = line.FirstTextSourceIndex + 1;
            Assert.True(Press(box, Key.End).Handled);
            Assert.Equal(end, box.CaretIndex);
            var precedingY = presenter.CursorRectangle.Y;
            Assert.InRange(precedingY, lines[0].Height - 0.5, lines[0].Height + 0.5);

            presenter.MoveCaretToTextPosition(end, trailingEdge: false);
            Assert.Equal(end, presenter.CaretIndex);
            Assert.True(presenter.CursorRectangle.Y > precedingY);
            presenter.MoveCaretToTextPosition(end, trailingEdge: true);
            Assert.Equal(end, presenter.CaretIndex);
            Assert.InRange(presenter.CursorRectangle.Y, precedingY - 0.5, precedingY + 0.5);
        }
    }

    [Theory]
    [InlineData("abcd \nnext", 1)]
    [InlineData("abcd \r\nnext", 2)]
    public void End_Before_Hard_Newline_Retains_Trailing_Space(string text, int newlineLength)
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToCaret);
        using (UnitTestApplication.Start(Services))
        {
            var (box, _, presenter) = CreateBox(text);
            Assert.Equal(newlineLength, presenter.TextLayout.TextLines[0].NewLineLength);
            box.CaretIndex = 2;
            Assert.True(Press(box, Key.End).Handled);
            Assert.Equal(5, box.CaretIndex);
        }
    }

    [Fact]
    public void CtrlEnd_After_SelectAll_Does_Not_Jump_To_Start()
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToCaret);
        using (UnitTestApplication.Start(Services))
        {
            var (box, _, _) = CreateBox(FourLines);
            box.CaretIndex = box.Text!.Length;
            Assert.True(Press(box, Key.A, KeyModifiers.Control).Handled);
            Assert.True(Press(box, Key.End, KeyModifiers.Control).Handled);
            Assert.Equal(box.Text.Length, box.CaretIndex);
            Assert.Equal(box.CaretIndex, box.SelectionStart);
            Assert.Equal(box.CaretIndex, box.SelectionEnd);
        }
    }

    [Fact]
    public void Programmatic_Caret_Assignments_Still_Work_After_End()
    {
        using var policy = UsePolicy(TextBox.HomeEndBehaviors.RelativeToCaret);
        using (UnitTestApplication.Start(Services))
        {
            var (box, _, presenter) = CreateBox("one two three four");
            box.CaretIndex = 2;
            Assert.True(Press(box, Key.End).Handled);
            Assert.Equal(box.Text!.Length, presenter.CaretIndex);

            box.CaretIndex = 1;
            Assert.Equal(1, presenter.CaretIndex);
            box.CaretIndex = box.Text.Length;
            Assert.Equal(box.Text.Length, presenter.CaretIndex);
        }
    }

    private static IDisposable UsePolicy(TextBox.HomeEndBehaviors mode)
    {
        var previous = TextBox.HomeEndBehavior;
        TextBox.HomeEndBehavior = mode;
        return new RestorePolicy(previous);
    }

    private sealed class RestorePolicy(TextBox.HomeEndBehaviors previous) : IDisposable
    {
        public void Dispose() => TextBox.HomeEndBehavior = previous;
    }

    private static void Select(TextBox box, int caret, int start, int end)
    {
        box.CaretIndex = caret;
        box.SelectionStart = start;
        box.SelectionEnd = end;
    }

    private static void AssertGesture(IEnumerable<KeyGesture> gestures, Key key, KeyModifiers modifiers) =>
        Assert.Contains(gestures, x => x.Key == key && x.KeyModifiers == modifiers);

    private static void AppleKeymap() =>
        AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>()
            .ToConstant(PlatformHotkeyConfiguration.Apple);

    private static KeyEventArgs Press(TextBox box, Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        var e = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = key,
            KeyModifiers = modifiers
        };
        box.RaiseEvent(e);
        return e;
    }

    private static (TextBox box, TestRoot root, TextPresenter presenter) CreateBox(
        string text, double width = 240, double height = 120,
        TextWrapping wrapping = TextWrapping.NoWrap)
    {
        var box = new TextBox
        {
            Template = TextBoxTests.CreateTemplate(),
            Text = text,
            TextWrapping = wrapping,
            AcceptsReturn = true,
            Width = width,
            Height = height
        };
        var root = new TestRoot
        {
            ClientSize = new Size(width, height),
            Child = box
        };
        box.ApplyTemplate();
        root.ExecuteInitialLayoutPass();
        var presenter = box.FindDescendantOfType<TextPresenter>();
        Assert.NotNull(presenter);
        return (box, root, presenter);
    }

    private static TestServices Services => TestServices.MockThreadingInterface.With(
        standardCursorFactory: Mock.Of<ICursorFactory>(),
        renderInterface: new HeadlessPlatformRenderInterface(),
        textShaperImpl: new HarfBuzzTextShaper(),
        fontManagerImpl: new TestFontManager(),
        assetLoader: new StandardAssetLoader());
}
