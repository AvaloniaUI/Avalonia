using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation
{
    public class AutomationTextRangeTests
    {
        public class Move : AutomationTextRangeTests
        {
            [Theory]
            [ClassData(typeof(Newlines))]
            public void Moves_Right_One_Char(string newline)
            {
                var peer = new TestPeer(newline);
                var range = new AutomationTextRange(peer, 0, 1);

                var result = range.Move(TextUnit.Character, 1);

                Assert.Equal(1, result);
                Assert.Equal(peer.Lines[0][1].ToString(), range.GetText(-1));
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Does_Not_Move_Right_From_Last_Char(string newline)
            {
                var peer = new TestPeer(newline);
                var start = peer.Text.Length - 1;
                var range = new AutomationTextRange(peer, start, start + 1);

                var result = range.Move(TextUnit.Character, 1);

                Assert.Equal(0, result);
                Assert.Equal(peer.Text[^1].ToString(), range.GetText(-1));
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Moves_Degenerate_Right_One_Char(string newline)
            {
                var peer = new TestPeer(newline);
                var range = new AutomationTextRange(peer, 0, 0);

                var result = range.Move(TextUnit.Character, 1);

                Assert.Equal(1, result);
                Assert.Equal(1, range.Start);
                Assert.Equal(1, range.End);
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Moves_Right_To_Newline(string newline)
            {
                var peer = new TestPeer(newline);
                var range = new AutomationTextRange(peer, 0, 1);

                var result = range.Move(TextUnit.Character, 1);

                Assert.Equal(1, result);
                Assert.Equal(peer.Lines[0][1].ToString(), range.GetText(-1));
            }

            [Theory(Skip = "Not yet implemented")]
            [ClassData(typeof(Newlines))]
            public void Moves_Right_One_Surrogate_Pair(string newline)
            {
                var peer = new TestPeer(newline);
                var start = peer.LineIndex(6);
                var range = new AutomationTextRange(peer, start, start + 2);

                Assert.Equal("🌉", range.GetText(-1));

                var result = range.Move(TextUnit.Character, 1);

                Assert.Equal(1, result);
                Assert.Equal("U", range.GetText(-1));
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Moves_Left_One_Char(string newline)
            {
                var peer = new TestPeer(newline);
                var range = new AutomationTextRange(peer, 0, 1);

                var result = range.Move(TextUnit.Character, 1);

                Assert.Equal(1, result);
                Assert.Equal(peer.Lines[0][1].ToString(), range.GetText(-1));
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Does_Not_Move_Left_From_First_Char(string newline)
            {
                var peer = new TestPeer(newline);
                var range = new AutomationTextRange(peer, 0, 1);

                var result = range.Move(TextUnit.Character, -1);

                Assert.Equal(0, result);
                Assert.Equal(peer.Text[0].ToString(), range.GetText(-1));
            }

            [Theory(Skip = "Not yet implemented")]
            [ClassData(typeof(Newlines))]
            public void Moves_Left_One_Surrogate_Pair(string newline)
            {
                var peer = new TestPeer(newline);
                var start = peer.LineIndex(6) + 2;
                var range = new AutomationTextRange(peer, start, start + 1);

                var result = range.Move(TextUnit.Character, -1);

                Assert.Equal(-1, result);
                Assert.Equal("🌉", range.GetText(-1));
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Moves_Down_To_Empty_Line(string newline)
            {
                var peer = new TestPeer(newline);
                var range = new AutomationTextRange(peer, 0, peer.Lines[0].Length);

                var result = range.Move(TextUnit.Line, 1);

                Assert.Equal(1, result);
                Assert.Equal(peer.Lines[1], range.GetText(-1));
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Moves_Degenerate_Down_To_Empty_Line(string newline)
            {
                var peer = new TestPeer(newline);
                var range = new AutomationTextRange(peer, 0, 0);

                var result = range.Move(TextUnit.Line, 1);

                Assert.Equal(1, result);
                Assert.Equal(peer.Lines[0].Length, range.Start);
                Assert.Equal(range.Start, range.End);
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Moves_Down_From_Mid_Line_To_Next_Line(string newline)
            {
                var peer = new TestPeer(newline);
                var range = new AutomationTextRange(peer, 2, 4);

                var result = range.Move(TextUnit.Line, 1);

                Assert.Equal(1, result);
                Assert.Equal(peer.Lines[1], range.GetText(-1));
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Does_Not_Move_Down_From_Last_Line(string newline)
            {
                var peer = new TestPeer(newline);
                var start = peer.LineIndex(peer.Lines.Length - 1);
                var range = new AutomationTextRange(peer, start, peer.Text.Length);

                var result = range.Move(TextUnit.Line, 1);

                Assert.Equal(0, result);
                Assert.Equal(peer.Lines[^1], range.GetText(-1));
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Moves_Up_To_Empty_Line(string newline)
            {
                var peer = new TestPeer(newline);
                var start = peer.LineIndex(2);
                var range = new AutomationTextRange(peer, start, start + peer.Lines[2].Length);

                var result = range.Move(TextUnit.Line, -1);

                Assert.Equal(-1, result);
                Assert.Equal(peer.Lines[1], range.GetText(-1));
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Moves_Degenerate_Up_To_Empty_Line(string newline)
            {
                var peer = new TestPeer(newline);
                var range = new AutomationTextRange(peer, peer.LineIndex(2), peer.LineIndex(2));

                var result = range.Move(TextUnit.Line, -1);

                Assert.Equal(-1, result);
                Assert.Equal(peer.Lines[0].Length, range.Start);
                Assert.Equal(range.Start, range.End);
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Moves_Up_From_Mid_Line_To_Next_Line(string newline)
            {
                var peer = new TestPeer(newline);
                var start = peer.LineIndex(2) + 2;
                var range = new AutomationTextRange(peer, start, start + 2);

                var result = range.Move(TextUnit.Line, -1);

                Assert.Equal(-1, result);
                Assert.Equal(peer.Lines[1], range.GetText(-1));
            }

            [Theory]
            [ClassData(typeof(Newlines))]
            public void Does_Not_Move_Up_From_First_Line(string newline)
            {
                var peer = new TestPeer(newline);
                var range = new AutomationTextRange(peer, 0, peer.Lines[0].Length);

                var result = range.Move(TextUnit.Line, -1);

                Assert.Equal(0, result);
                Assert.Equal(peer.Lines[0], range.GetText(-1));
            }
        }

        private class TestPeer : ITextPeer
        {
            private readonly string[] _lines;

            public TestPeer(string newline)
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
                    "commodo consequat.",
                };
            }

            public bool IsReadOnly => true;
            public int LineCount => _lines.Length;
            public string[] Lines => _lines;
            public string Text => string.Join(null, _lines);

            public IReadOnlyList<Rect> GetBounds(int start, int end)
            {
                throw new NotImplementedException();
            }

            public int LineFromChar(int charIndex)
            {
                var l = 0;
                var c = 0;

                foreach (var line in _lines)
                {
                    if ((c += line.Length) > charIndex)
                        return l;
                    ++l;
                }

                return l;
            }

            public int LineIndex(int lineIndex)
            {
                var c = 0;
                var l = 0;

                foreach (var line in _lines)
                {
                    if (l++ == lineIndex)
                        break;
                    c += line.Length;
                }

                return c;
            }

            public void ScrollIntoView(int start, int end)
            {
                throw new NotImplementedException();
            }

            public void Select(int start, int end)
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
}
