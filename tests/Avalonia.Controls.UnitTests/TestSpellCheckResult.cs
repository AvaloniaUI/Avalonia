using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;

namespace Avalonia.Controls.UnitTests;

internal sealed class TestSpellCheckResult : ISpellCheckResult
{
    private readonly IReadOnlyList<string> _suggestions;

    public TestSpellCheckResult(
        int start,
        int length,
        string? word = null,
        IReadOnlyList<string>? suggestions = null)
    {
        Start = start;
        Length = length;
        Word = word;
        _suggestions = suggestions ?? Array.Empty<string>();
    }

    public int Start { get; }

    public int Length { get; }

    public string? Description => null;

    public string? Word { get; }

    public ValueTask<IReadOnlyList<string>> SuggestAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<IReadOnlyList<string>>(_suggestions);
    }

    public override bool Equals(object? obj) =>
        obj is TestSpellCheckResult other &&
        Start == other.Start &&
        Length == other.Length &&
        string.Equals(Word, other.Word, StringComparison.Ordinal);

    public override int GetHashCode() => HashCode.Combine(Start, Length, Word);
}
