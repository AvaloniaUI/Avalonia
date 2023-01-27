using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Win32.Automation;
using Avalonia.Win32.Interop.Automation;
using Xunit;
using AAP = Avalonia.Automation.Provider;

namespace Avalonia.Win32.UnitTests.Automation;

public class AutomationTextRangeTests
{
    public class Move : AutomationTextRangeTests
    {
        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Right_One_Char(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var range = new AutomationTextRange(node, 0, 1);

            var result = range.Move(TextUnit.Character, 1);

            Assert.Equal(1, result);
            Assert.Equal(peer.Lines[0][1].ToString(), range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Does_Not_Move_Right_From_Last_Char(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var start = peer.Text.Length - 1;
            var range = new AutomationTextRange(node, start, start + 1);

            var result = range.Move(TextUnit.Character, 1);

            Assert.Equal(0, result);
            Assert.Equal(peer.Text[^1].ToString(), range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Degenerate_Right_One_Char(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var range = new AutomationTextRange(node, 0, 0);

            var result = range.Move(TextUnit.Character, 1);

            Assert.Equal(1, result);
            Assert.Equal(1, range.Start);
            Assert.Equal(1, range.End);
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Right_To_Newline(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var range = new AutomationTextRange(node, 0, 1);

            var result = range.Move(TextUnit.Character, 1);

            Assert.Equal(1, result);
            Assert.Equal(peer.Lines[0][1].ToString(), range.GetText(-1));
        }

        [Theory(Skip = "Not yet implemented")]
        [ClassData(typeof(Newlines))]
        public void Moves_Right_One_Surrogate_Pair(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var start = peer.GetLineRange(6).Start;
            var range = new AutomationTextRange(node, start, start + 2);

            Assert.Equal("🌉", range.GetText(-1));

            var result = range.Move(TextUnit.Character, 1);

            Assert.Equal(1, result);
            Assert.Equal("U", range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Left_One_Char(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var range = new AutomationTextRange(node, 0, 1);

            var result = range.Move(TextUnit.Character, 1);

            Assert.Equal(1, result);
            Assert.Equal(peer.Lines[0][1].ToString(), range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Does_Not_Move_Left_From_First_Char(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var range = new AutomationTextRange(node, 0, 1);

            var result = range.Move(TextUnit.Character, -1);

            Assert.Equal(0, result);
            Assert.Equal(peer.Text[0].ToString(), range.GetText(-1));
        }

        [Theory(Skip = "Not yet implemented")]
        [ClassData(typeof(Newlines))]
        public void Moves_Left_One_Surrogate_Pair(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var start = peer.GetLineRange(6).Start + 2;
            var range = new AutomationTextRange(node, start, start + 1);

            var result = range.Move(TextUnit.Character, -1);

            Assert.Equal(-1, result);
            Assert.Equal("🌉", range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Right_One_Word_With_Trailing_Newlines(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var range = new AutomationTextRange(node, 0, 1);

            var result = range.Move(TextUnit.Word, 1);

            Assert.Equal(1, result);

            // From https://docs.microsoft.com/en-us/windows/win32/winauto/uiauto-uiautomationtextunits
            // When TextUnit_Word is used to set the boundary of a text range, the resulting text range
            // should include any word break characters that are present at the end of the word, but
            // before the start of the next word.
            Assert.Equal("text:" + newline + newline, range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Right_One_Word_With_Apostrophe(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var start = peer.GetLineRange(10).Start;
            var range = new AutomationTextRange(node, start, start + 1);

            var result = range.Move(TextUnit.Word, 1);

            Assert.Equal(1, result);

            Assert.Equal("can't ", range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Left_To_Previous_Line_With_Trailing_Newlines(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var start = peer.GetLineRange(2).Start;
            var range = new AutomationTextRange(node, start, start + 1);

            var result = range.Move(TextUnit.Word, -1);

            Assert.Equal(-1, result);
            Assert.Equal("text:" + newline + newline, range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Down_To_Empty_Line(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var range = new AutomationTextRange(node, 0, peer.Lines[0].Length);

            var result = range.Move(TextUnit.Line, 1);

            Assert.Equal(1, result);
            Assert.Equal(peer.Lines[1], range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Degenerate_Down_To_Empty_Line(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var range = new AutomationTextRange(node, 0, 0);

            var result = range.Move(TextUnit.Line, 1);

            Assert.Equal(1, result);
            Assert.Equal(peer.Lines[0].Length, range.Start);
            Assert.Equal(range.Start, range.End);
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Down_From_Mid_Line_To_Next_Line(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var range = new AutomationTextRange(node, 2, 4);

            var result = range.Move(TextUnit.Line, 1);

            Assert.Equal(1, result);
            Assert.Equal(peer.Lines[1], range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Does_Not_Move_Down_From_Last_Line(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var start = peer.GetLineRange(peer.Lines.Count - 1).Start;
            var range = new AutomationTextRange(node, start, peer.Text.Length);

            var result = range.Move(TextUnit.Line, 1);

            Assert.Equal(0, result);
            Assert.Equal(peer.Lines[^1], range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Up_To_Empty_Line(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var start = peer.GetLineRange(2).Start;
            var range = new AutomationTextRange(node, start, start + peer.Lines[2].Length);

            var result = range.Move(TextUnit.Line, -1);

            Assert.Equal(-1, result);
            Assert.Equal(peer.Lines[1], range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Degenerate_Up_To_Empty_Line(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var start = peer.GetLineRange(2).Start;
            var range = new AutomationTextRange(node, start, start);

            var result = range.Move(TextUnit.Line, -1);

            Assert.Equal(-1, result);
            Assert.Equal(peer.Lines[0].Length, range.Start);
            Assert.Equal(range.Start, range.End);
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Moves_Up_From_Mid_Line_To_Next_Line(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var start = peer.GetLineRange(2).Start + 2;
            var range = new AutomationTextRange(node, start, start + 2);

            var result = range.Move(TextUnit.Line, -1);

            Assert.Equal(-1, result);
            Assert.Equal(peer.Lines[1], range.GetText(-1));
        }

        [Theory]
        [ClassData(typeof(Newlines))]
        public void Does_Not_Move_Up_From_First_Line(string newline)
        {
            var (node, peer) = CreateTestNode(newline);
            var range = new AutomationTextRange(node, 0, peer.Lines[0].Length);

            var result = range.Move(TextUnit.Line, -1);

            Assert.Equal(0, result);
            Assert.Equal(peer.Lines[0], range.GetText(-1));
        }
    }

    private static (AutomationNode, TestPeer) CreateTestNode(string newline)
    {
        var peer = new TestPeer(newline);
        return (new AutomationNode(peer), peer);
    }

    private class TestPeer : ControlAutomationPeer, AAP.ITextProvider
    {
        private readonly string[] _lines;

        public TestPeer(string newline)
            : base(new Control())
        {
            _lines = new[]
            {
                "Test text:" + newline,
                newline,
                "Lorem ipsum dolor sit amet, ",
                "consectetur adipiscing elit, ",
                "sed do eiusmod tempor incididunt ",
                "ut labore et dolore magna aliqua." + newline,
                "🌉Ut enim ad minim veniam, quis ",
                "nostrud exercitation ullamco ",
                "laboris nisi ut aliquip ex ea ",
                "commodo consequat." + newline,
                "We can't stop now"
            };
        }

        public bool IsReadOnly => true;
        public int CaretIndex => 0;
        public TextRange DocumentRange => new(0, _lines.Sum(x => x.Length));
        public IList<string> Lines => _lines;
        public int LineCount => _lines.Length;
        public string PlaceholderText => string.Empty;
        public AAP.SupportedTextSelection SupportedTextSelection { get; }
        public string Text => string.Concat(_lines);

        public event EventHandler SelectedRangesChanged;
        public event EventHandler TextChanged;

        public IReadOnlyList<Rect> GetBounds(TextRange range)
        {
            throw new NotImplementedException();
        }

        public int GetLineForIndex(int index)
        {
            var i = 0;

            for (var line = 0; line < _lines.Length; ++line)
            {
                i += _lines[line].Length;
                if (index < i)
                    return line;
            }

            return -1;
        }

        public TextRange GetLineRange(int lineIndex)
        {
            var start = _lines.Take(lineIndex).Sum(x => x.Length);
            var end = start + _lines[lineIndex].Length;
            return new TextRange(start, end);
        }

        public IReadOnlyList<TextRange> GetSelection()
        {
            throw new NotImplementedException();
        }

        public string GetText(TextRange range)
        {
            var text = Text;
            var end = Math.Min(range.End, text.Length);
            return text.Substring(range.Start, end - range.Start);
        }

        public TextRange RangeFromPoint(Point p)
        {
            throw new NotImplementedException();
        }

        public void ScrollIntoView(TextRange range)
        {
            throw new NotImplementedException();
        }

        public void Select(TextRange range)
        {
            throw new NotImplementedException();
        }
    }

    private class Newlines : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "\r" };
            yield return new object[] { "\n" };
            yield return new object[] { "\r\n" };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
