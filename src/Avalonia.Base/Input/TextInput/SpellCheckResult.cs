using Avalonia.Metadata;

namespace Avalonia.Input.TextInput;

/// <summary>
/// Represents a misspelled text range.
/// </summary>
/// <param name="Start">
/// The zero-based UTF-16 offset within the text passed to <see cref="ISpellCheckProvider.CheckAsync"/>.
/// </param>
/// <param name="Length">The UTF-16 length of the misspelled range.</param>
/// <param name="Word">
/// The misspelled word, or null to let the caller extract it.
/// </param>
[Unstable("SpellCheckResult is in early development and may change in minor releases.")]
public readonly record struct SpellCheckResult(int Start, int Length, string? Word = null);
