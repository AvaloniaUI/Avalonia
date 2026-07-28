using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.TextFormatting;

/// <summary>
/// Walks the runs of a <see cref="TextLine"/> in <b>logical</b> (source-text)
/// order. This is the order that splits and length-based offsets are defined
/// in, and is what every <see cref="TextCollapsingProperties.Collapse"/>
/// implementation needs to see — unlike <see cref="TextLine.TextRuns"/>,
/// which exposes the post-BiDi <i>visual</i> ordering used for rendering.
/// </summary>
/// <remarks>
/// When the line has been finalized (the normal case after
/// <c>TextLineImpl.FinalizeLine</c>), the enumerator iterates over
/// <c>_indexedTextRuns</c> — a level-resolved table that maps each run back
/// to its original logical position. If the line hasn't been finalized (or
/// the line is not a <c>TextLineImpl</c>), it falls back to the raw
/// <see cref="TextLine.TextRuns"/> list.
/// </remarks>
internal ref struct LogicalTextRunEnumerator
{
    private readonly IReadOnlyList<TextRun>? _textRuns;
    private readonly IReadOnlyList<IndexedTextRun>? _indexedTextRuns;

    private readonly int _step;
    private readonly int _end;

    private int _index;

    public int Count { get; }

    public LogicalTextRunEnumerator(TextLine line, bool backward = false)
    {
        var indexedTextRuns = (line as TextLineImpl)?._indexedTextRuns;

        if (indexedTextRuns?.Count > 0)
        {
            _indexedTextRuns = indexedTextRuns;
            Count = indexedTextRuns.Count;
        }
        else if (line.TextRuns.Count > 0)
        {
            _textRuns = line.TextRuns;
            Count = _textRuns.Count;
        }

        if (backward)
        {
            _step = -1;
            _end = -1;
            _index = Count;
        }
        else
        {
            _step = 1;
            _end = Count;
            _index = -1;
        }
    }

    public bool MoveNext([MaybeNullWhen(false)] out TextRun run)
    {
        _index += _step;

        if (_index == _end)
        {
            run = null;

            return false;
        }

        if (_indexedTextRuns != null)
        {
            run = _indexedTextRuns[_index].TextRun!;
        }
        else if (_textRuns != null)
        {
            run = _textRuns[_index];
        }
        else
        {
            run = null;

            return false;
        }

        return true;
    }
}
