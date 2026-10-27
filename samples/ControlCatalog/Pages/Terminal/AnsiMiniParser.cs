using System;
using System.Collections.Generic;
using System.Text;

namespace ControlCatalog.Pages.Terminal
{
    /// <summary>
    /// Parses output for the <c>ansi-mini</c> terminfo entry: cup, home, clear, ed, el, cr, cud1, ind and bel.
    /// Cursor moves and ECH are also handled, because ConPTY sends them. Every other sequence is skipped.
    /// </summary>
    internal sealed class AnsiMiniParser
    {
        private enum State
        {
            Ground,
            Escape,
            EscapeIntermediate,
            Csi,
            Osc,
            OscEscape
        }

        private readonly TerminalBuffer _buffer;
        private readonly Decoder _decoder = new UTF8Encoding(false).GetDecoder();
        private readonly List<int> _parameters = new();
        private char[] _chars = new char[4096];
        private State _state;
        private int _currentParameter = -1;
        private bool _csiIgnored;
        private char _highSurrogate;

        public AnsiMiniParser(TerminalBuffer buffer) => _buffer = buffer;

        public void Feed(ReadOnlySpan<byte> data)
        {
            var needed = _decoder.GetCharCount(data, false);
            if (_chars.Length < needed)
                _chars = new char[needed];
            var count = _decoder.GetChars(data, _chars, false);
            for (var i = 0; i < count; i++)
                Feed(_chars[i]);
        }

        private void Feed(char c)
        {
            // C0 controls are executed in every state except OSC, like on a VT100
            if (c < 0x20 && _state is not (State.Osc or State.OscEscape))
            {
                Execute(c);
                return;
            }

            switch (_state)
            {
                case State.Ground:
                    Print(c);
                    break;
                case State.Escape:
                    EscapeDispatch(c);
                    break;
                case State.EscapeIntermediate:
                    if (c >= 0x30)
                        _state = State.Ground;
                    break;
                case State.Csi:
                    CsiCollect(c);
                    break;
                case State.Osc:
                    if (c == 0x1B)
                        _state = State.OscEscape;
                    else if (c == 0x07)
                        _state = State.Ground;
                    break;
                case State.OscEscape:
                    _state = c == '\\' ? State.Ground : State.Osc;
                    break;
            }
        }

        private void Execute(char c)
        {
            switch (c)
            {
                case '\x1b':
                    _state = State.Escape;
                    break;
                case '\r':
                    _buffer.CarriageReturn();
                    break;
                case '\n':
                case '\v':
                case '\f':
                    _buffer.LineFeed();
                    break;
                case '\b':
                    _buffer.Backspace();
                    break;
                case '\t':
                    _buffer.Tab();
                    break;
                case '\x18':
                case '\x1a':
                    // CAN and SUB cancel a sequence
                    _state = State.Ground;
                    break;
            }
        }

        private void Print(char c)
        {
            if (char.IsHighSurrogate(c))
            {
                _highSurrogate = c;
                return;
            }

            if (char.IsLowSurrogate(c))
            {
                if (_highSurrogate != 0)
                    _buffer.Print(new string(new[] { _highSurrogate, c }));
                _highSurrogate = '\0';
                return;
            }

            _highSurrogate = '\0';
            if (c != 0x7F)
                _buffer.Print(c.ToString());
        }

        private void EscapeDispatch(char c)
        {
            _state = State.Ground;
            switch (c)
            {
                case '[':
                    _state = State.Csi;
                    _parameters.Clear();
                    _currentParameter = -1;
                    _csiIgnored = false;
                    break;
                case ']':
                    _state = State.Osc;
                    break;
                case '7':
                    _buffer.SaveCursor();
                    break;
                case '8':
                    _buffer.RestoreCursor();
                    break;
                case 'D':
                    _buffer.LineFeed();
                    break;
                case 'E':
                    _buffer.CarriageReturn();
                    _buffer.LineFeed();
                    break;
                default:
                    if (c is >= ' ' and <= '/')
                        _state = State.EscapeIntermediate;
                    break;
            }
        }

        private void CsiCollect(char c)
        {
            if (c is >= '0' and <= '9')
            {
                _currentParameter = Math.Min(9999, Math.Max(0, _currentParameter) * 10 + (c - '0'));
            }
            else if (c == ';')
            {
                _parameters.Add(_currentParameter);
                _currentParameter = -1;
            }
            else if (c is >= '<' and <= '?' or >= ' ' and <= '/' or ':')
            {
                // Private markers (ESC[?25l) and intermediates are not part of ansi-mini
                _csiIgnored = true;
            }
            else if (c is >= '@' and <= '~')
            {
                _parameters.Add(_currentParameter);
                _state = State.Ground;
                if (!_csiIgnored)
                    CsiDispatch(c);
            }
            else
            {
                _state = State.Ground;
            }
        }

        private int Parameter(int index, int defaultValue)
        {
            var value = index < _parameters.Count ? _parameters[index] : -1;
            return value <= 0 ? defaultValue : value;
        }

        private void CsiDispatch(char final)
        {
            switch (final)
            {
                case 'H':
                case 'f':
                    _buffer.MoveTo(Parameter(0, 1) - 1, Parameter(1, 1) - 1);
                    break;
                case 'J':
                    _buffer.EraseInDisplay(Math.Max(0, _parameters[0]));
                    break;
                case 'K':
                    _buffer.EraseInLine(Math.Max(0, _parameters[0]));
                    break;
                case 'A':
                    _buffer.MoveBy(-Parameter(0, 1), 0);
                    break;
                case 'B':
                    _buffer.MoveBy(Parameter(0, 1), 0);
                    break;
                case 'C':
                    _buffer.MoveBy(0, Parameter(0, 1));
                    break;
                case 'D':
                    _buffer.MoveBy(0, -Parameter(0, 1));
                    break;
                case 'G':
                    _buffer.MoveTo(_buffer.CursorY, Parameter(0, 1) - 1);
                    break;
                case 'd':
                    _buffer.MoveTo(Parameter(0, 1) - 1, _buffer.CursorX);
                    break;
                case 'X':
                    _buffer.EraseChars(Parameter(0, 1));
                    break;
            }
        }
    }
}
