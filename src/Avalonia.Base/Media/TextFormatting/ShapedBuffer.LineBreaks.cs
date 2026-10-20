using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting;

public sealed partial class ShapedBuffer
{
    // Text is immutable for a shaped buffer. Retain Unicode opportunities in
    // original-text coordinates, just as cluster widths retain their prefix
    // coordinates. A wrapped tail must not scan the remaining word again.
    private SourceLineBreakCache? _lineBreaks;
    private int _lineBreakStart;

    internal CachedLineBreakEnumerator GetCachedLineBreakEnumerator()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ShapedBuffer));
        _lineBreaks ??= new SourceLineBreakCache(Text);
        return new CachedLineBreakEnumerator(_lineBreaks, _lineBreakStart, Text);
    }

    internal sealed class SourceLineBreakCache
    {
        public readonly LineBreak[] Breaks;
        public SourceLineBreakCache(ReadOnlyMemory<char> text)
        {
            var values = new List<LineBreak>();
            var enumerator = new LineBreakEnumerator(text.Span);
            while (enumerator.MoveNext(out var value)) values.Add(value);
            Breaks = values.ToArray();
        }
    }

    internal struct CachedLineBreakEnumerator
    {
        private readonly LineBreak[]? _breaks;
        private readonly int _start, _end;
        private readonly LineBreak _terminal;
        private int _index;
        private bool _terminalPending;

        internal CachedLineBreakEnumerator(SourceLineBreakCache cache, int start, ReadOnlyMemory<char> text)
        {
            _breaks = cache.Breaks;
            _start = start;
            _end = start + text.Length;
            int low = 0, high = _breaks.Length;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (_breaks[middle].PositionWrap <= start) low = middle + 1;
                else high = middle;
            }
            _index = low;
            _terminal = default;
            _terminalPending = false;
            if (text.IsEmpty) return;

            // A leading split can end inside an original word. It still needs
            // its own terminal opportunity. Wrapped trailing buffers keep the
            // original end and take the cached path, avoiding repeated scans.
            low = _index; high = _breaks.Length;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (_breaks[middle].PositionWrap < _end) low = middle + 1;
                else high = middle;
            }
            if (low == _breaks.Length || _breaks[low].PositionWrap != _end)
            {
                var terminal = new LineBreakEnumerator(text.Span);
                while (terminal.MoveNext(out var value)) _terminal = value;
                _terminalPending = true;
            }
        }

        public bool MoveNext(out LineBreak lineBreak)
        {
            if (_breaks is not null && _index < _breaks.Length && _breaks[_index].PositionWrap <= _end)
            {
                var value = _breaks[_index++];
                lineBreak = new LineBreak(Math.Max(0, value.PositionMeasure - _start),
                    value.PositionWrap - _start, value.Required);
                return true;
            }
            if (_terminalPending)
            {
                _terminalPending = false;
                lineBreak = _terminal;
                return true;
            }
            lineBreak = default;
            return false;
        }
    }
}
