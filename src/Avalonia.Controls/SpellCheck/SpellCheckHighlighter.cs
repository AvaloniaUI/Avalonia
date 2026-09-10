using System.Collections.Generic;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;

namespace Avalonia.Controls;

internal sealed class SpellCheckHighlighter
{
    private List<SpellCheckResult>? _visible;
    private List<SpellCheckResult>? _spare;

    public void Clear(TextPresenter? presenter)
    {
        presenter?.SetSpellCheckRanges(null);

        if (_visible is not null)
        {
            _visible.Clear();
            _spare = _visible;
            _visible = null;
        }
    }

    // Hide the word being typed while the caret remains in it.
    public void Apply(
        TextPresenter presenter,
        IReadOnlyList<SpellCheckResult> results,
        List<SpellCheckRange> visibleRanges,
        SpellCheckRange? typingWord = null,
        int caretIndex = -1)
    {
        // Reuse the spare list without changing the one still held by the presenter.
        var visible = _spare ?? new List<SpellCheckResult>(_visible?.Capacity ?? 4);
        visible.Clear();
        var rangeIndex = 0;

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var resultEnd = result.Start + result.Length;

            // Both collections are sorted, so scan the visible ranges only once.
            while (rangeIndex < visibleRanges.Count && visibleRanges[rangeIndex].End <= result.Start)
            {
                rangeIndex++;
            }

            if (rangeIndex >= visibleRanges.Count)
            {
                break;
            }

            if (resultEnd <= visibleRanges[rangeIndex].Start || IsBeingTyped(result, typingWord, caretIndex))
            {
                continue;
            }

            visible.Add(result);
        }

        if (presenter.SetSpellCheckRanges(visible))
        {
            var previous = _visible;
            _visible = visible.Count == 0 ? null : visible;
            _spare = previous;
        }
        else
        {
            _spare = visible;
        }
    }

    private static bool IsBeingTyped(SpellCheckResult result, SpellCheckRange? typingWord, int caretIndex)
    {
        if (typingWord is not { } word)
        {
            return false;
        }

        var resultEnd = result.Start + result.Length;

        return resultEnd > word.Start && result.Start < word.End &&
            caretIndex > word.Start && caretIndex <= word.End;
    }
}
