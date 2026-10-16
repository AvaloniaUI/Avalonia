using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Avalonia.Threading;

namespace Avalonia.Controls;

// Checks mixed language text: a word is an error only when every language reports it.
internal sealed class CompositeSpellCheckContext : ISpellCheckContext
{
    private readonly IReadOnlyList<ISpellCheckContext> _contexts;
    private bool _disposed;

    public CompositeSpellCheckContext(IReadOnlyList<ISpellCheckContext> contexts)
    {
        _contexts = contexts;
    }

    public async ValueTask<IReadOnlyList<ISpellCheckResult>> CheckAsync(
        ReadOnlyMemory<char> text,
        CancellationToken cancellationToken = default)
    {
        var perLanguage = new List<IReadOnlyList<ISpellCheckResult>>(_contexts.Count);

        for (var i = 0; i < _contexts.Count; i++)
        {
            if (i > 0)
            {
                // Native checks block the UI thread, so let input and rendering run between languages.
                await Dispatcher.Yield(DispatcherPriority.Background);
                cancellationToken.ThrowIfCancellationRequested();

                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }

            var results = await _contexts[i].CheckAsync(text, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // A language with no errors accepts every word, so nothing can be wrong in all of them.
            if (results.Count == 0)
            {
                return Array.Empty<ISpellCheckResult>();
            }

            perLanguage.Add(results);
        }

        List<ISpellCheckResult>? common = null;

        foreach (var result in perLanguage[0])
        {
            var matches = new ISpellCheckResult[perLanguage.Count];
            matches[0] = result;
            var inAll = true;

            for (var i = 1; i < perLanguage.Count; i++)
            {
                if (FindOverlapping(perLanguage[i], result) is not { } match)
                {
                    inAll = false;
                    break;
                }

                matches[i] = match;
            }

            if (inAll)
            {
                (common ??= new List<ISpellCheckResult>()).Add(new CompositeResult(matches));
            }
        }

        return common is null ? Array.Empty<ISpellCheckResult>() : common;
    }

    public void Dispose()
    {
        _disposed = true;

        foreach (var context in _contexts)
        {
            context.Dispose();
        }
    }

    // Backends may split words slightly differently per language, so any overlap counts as the same word.
    private static ISpellCheckResult? FindOverlapping(IReadOnlyList<ISpellCheckResult> results, ISpellCheckResult target)
    {
        foreach (var result in results)
        {
            if (result.Start < target.Start + target.Length && target.Start < result.Start + result.Length)
            {
                return result;
            }
        }

        return null;
    }

    private sealed class CompositeResult : ISpellCheckResult
    {
        private readonly ISpellCheckResult[] _results;

        public CompositeResult(ISpellCheckResult[] results)
        {
            _results = results;
        }

        public int Start => _results[0].Start;

        public int Length => _results[0].Length;

        public string? Description => _results[0].Description;

        // Interleave so every language contributes before the suggestion list is trimmed.
        public async ValueTask<IReadOnlyList<string>> SuggestAsync(CancellationToken cancellationToken = default)
        {
            var lists = new IReadOnlyList<string>[_results.Length];

            for (var i = 0; i < _results.Length; i++)
            {
                lists[i] = await _results[i].SuggestAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            var merged = new List<string>();

            for (var index = 0; ; index++)
            {
                var any = false;

                foreach (var list in lists)
                {
                    if (index < list.Count)
                    {
                        any = true;

                        if (!merged.Contains(list[index]))
                        {
                            merged.Add(list[index]);
                        }
                    }
                }

                if (!any)
                {
                    break;
                }
            }

            return merged;
        }
    }
}
