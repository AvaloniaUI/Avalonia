namespace Avalonia.Input.TextInput;

/// <summary>
/// Represents a misspelled text range.
/// </summary>
/// <param name="Start">The zero-based UTF-16 start offset of the misspelled range.</param>
/// <param name="Length">The UTF-16 length of the misspelled range.</param>
/// <param name="Word">The misspelled word, when available.</param>
public readonly record struct SpellCheckResult(int Start, int Length, string? Word = null);
