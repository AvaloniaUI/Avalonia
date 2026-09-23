using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput;

/// <summary>
/// Spell checks the text of one text view and owns any platform resources needed to do so.
/// </summary>
/// <remarks>
/// Used from the thread that created it, one call at a time. Disposing it cancels pending calls and
/// invalidates its results.
/// </remarks>
[Unstable("ISpellCheckContext is in early development and may change in minor releases.")]
public interface ISpellCheckContext : IDisposable
{
    /// <summary>
    /// Checks <paramref name="text"/> and returns its errors.
    /// </summary>
    /// <param name="text">The text to check. Keep it unchanged until the call completes.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>Errors whose UTF-16 offsets are relative to <paramref name="text"/>.</returns>
    /// <exception cref="OperationCanceledException">The operation was cancelled. Partial results are never returned.</exception>
    /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
    ValueTask<IReadOnlyList<ISpellCheckResult>> CheckAsync(
        ReadOnlyMemory<char> text,
        CancellationToken cancellationToken = default);
}
