using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput;

/// <summary>
/// An error reported by <see cref="ISpellCheckContext.CheckAsync"/>.
/// </summary>
/// <remarks>
/// A result is not disposable and holds no platform resources. It stays usable until the context that produced
/// it is disposed.
/// </remarks>
[Unstable("ISpellCheckResult is in early development and may change in minor releases.")]
public interface ISpellCheckResult
{
    /// <summary>
    /// Gets the UTF-16 offset of the error, relative to the checked text.
    /// </summary>
    int Start { get; }

    /// <summary>
    /// Gets the UTF-16 length of the error.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Gets a description of the error supplied by the backend, such as a grammar explanation.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets replacement suggestions for this error.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <exception cref="OperationCanceledException">The operation was cancelled, or the context was disposed while it was pending.</exception>
    /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
    ValueTask<IReadOnlyList<string>> SuggestAsync(CancellationToken cancellationToken = default);
}
