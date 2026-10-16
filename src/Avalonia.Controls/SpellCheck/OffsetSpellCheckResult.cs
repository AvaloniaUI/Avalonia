using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;

namespace Avalonia.Controls;

// Moves a result to a new text position while keeping its suggestions.
internal sealed class OffsetSpellCheckResult : ISpellCheckResult
{
    private readonly ISpellCheckResult _source;

    private OffsetSpellCheckResult(ISpellCheckResult source, int start, int length)
    {
        _source = source;
        Start = start;
        Length = length;
    }

    public int Start { get; }

    public int Length { get; }

    public string? Description => _source.Description;

    public ValueTask<IReadOnlyList<string>> SuggestAsync(CancellationToken cancellationToken = default) =>
        _source.SuggestAsync(cancellationToken);

    public static ISpellCheckResult Create(ISpellCheckResult source, int start, int length)
    {
        if (source.Start == start && source.Length == length)
        {
            return source;
        }

        if (source is OffsetSpellCheckResult offset)
        {
            source = offset._source;
        }

        return new OffsetSpellCheckResult(source, start, length);
    }
}
