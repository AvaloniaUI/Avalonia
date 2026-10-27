using System;
using System.Text;

namespace ControlCatalog.Pages.Terminal
{
    /// <summary>
    /// A fixed-size character grid with a cursor. No scrollback, no attributes.
    /// </summary>
    internal sealed class TerminalBuffer
    {
        private string?[] _cells;
        // Deferred autowrap: a char written to the last column moves the cursor to the next line only when the next
        // printable char arrives.
        private bool _wrapPending;
        private int _savedX, _savedY;

        public TerminalBuffer(int columns, int rows)
        {
            Columns = columns;
            Rows = rows;
            _cells = new string?[columns * rows];
        }

        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public int CursorX { get; private set; }
        public int CursorY { get; private set; }

        public string? GetCell(int x, int y) => _cells[y * Columns + x];

        public string GetLine(int y)
        {
            var sb = new StringBuilder(Columns);
            for (var x = 0; x < Columns; x++)
                sb.Append(GetCell(x, y) ?? " ");
            return sb.ToString();
        }

        public string GetText()
        {
            var sb = new StringBuilder();
            for (var y = 0; y < Rows; y++)
                sb.AppendLine(GetLine(y).TrimEnd());
            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        public void Print(string grapheme)
        {
            if (_wrapPending)
            {
                CarriageReturn();
                LineFeed();
            }

            _cells[CursorY * Columns + CursorX] = grapheme;

            if (CursorX == Columns - 1)
                _wrapPending = true;
            else
                CursorX++;
        }

        public void CarriageReturn()
        {
            CursorX = 0;
            _wrapPending = false;
        }

        public void LineFeed()
        {
            _wrapPending = false;
            if (CursorY == Rows - 1)
                ScrollUp();
            else
                CursorY++;
        }

        public void Backspace()
        {
            _wrapPending = false;
            if (CursorX > 0)
                CursorX--;
        }

        public void Tab()
        {
            _wrapPending = false;
            CursorX = Math.Min(Columns - 1, (CursorX / 8 + 1) * 8);
        }

        public void MoveTo(int row, int column)
        {
            _wrapPending = false;
            CursorY = Math.Clamp(row, 0, Rows - 1);
            CursorX = Math.Clamp(column, 0, Columns - 1);
        }

        public void MoveBy(int rows, int columns) => MoveTo(CursorY + rows, CursorX + columns);

        public void SaveCursor() => (_savedX, _savedY) = (CursorX, CursorY);

        public void RestoreCursor() => MoveTo(_savedY, _savedX);

        public void EraseInDisplay(int mode)
        {
            var cursor = CursorY * Columns + CursorX;
            switch (mode)
            {
                case 0:
                    Clear(cursor, _cells.Length);
                    break;
                case 1:
                    Clear(0, cursor + 1);
                    break;
                case 2:
                case 3:
                    Clear(0, _cells.Length);
                    break;
            }
        }

        public void EraseInLine(int mode)
        {
            var start = CursorY * Columns;
            switch (mode)
            {
                case 0:
                    Clear(start + CursorX, start + Columns);
                    break;
                case 1:
                    Clear(start, start + CursorX + 1);
                    break;
                case 2:
                    Clear(start, start + Columns);
                    break;
            }
        }

        public void EraseChars(int count)
        {
            var start = CursorY * Columns + CursorX;
            Clear(start, start + Math.Min(count, Columns - CursorX));
        }

        public void Resize(int columns, int rows)
        {
            if (columns == Columns && rows == Rows)
                return;

            // Keep the bottom of the screen, where the cursor usually is
            var skip = Math.Max(0, CursorY - (rows - 1));
            var cells = new string?[columns * rows];
            for (var y = 0; y < rows && y + skip < Rows; y++)
                for (var x = 0; x < Math.Min(columns, Columns); x++)
                    cells[y * columns + x] = _cells[(y + skip) * Columns + x];

            _cells = cells;
            Columns = columns;
            Rows = rows;
            _wrapPending = false;
            CursorY -= skip;
            CursorX = Math.Min(CursorX, columns - 1);
        }

        private void ScrollUp()
        {
            Array.Copy(_cells, Columns, _cells, 0, _cells.Length - Columns);
            Clear(_cells.Length - Columns, _cells.Length);
        }

        private void Clear(int start, int end) => Array.Clear(_cells, start, end - start);
    }
}
