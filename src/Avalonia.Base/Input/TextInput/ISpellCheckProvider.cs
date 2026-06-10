using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Input.TextInput;

/// <summary>
/// Provides spell-checking services for text input controls.
/// </summary>
public interface ISpellCheckProvider
{
    /// <summary>
    /// Gets a value indicating whether spell checking is available for the specified culture.
    /// </summary>
    /// <param name="culture">The culture to check, or null for the current input culture.</param>
    /// <returns>True when spell checking is available; otherwise false.</returns>
    bool IsLanguageSupported(CultureInfo? culture);

    /// <summary>
    /// Checks text and returns the misspelled ranges.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <param name="culture">The culture to check, or null for the current input culture.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The misspelled text ranges.</returns>
    ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
        string text,
        CultureInfo? culture,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets replacement suggestions for a misspelled word.
    /// </summary>
    /// <param name="word">The misspelled word.</param>
    /// <param name="culture">The culture to check, or null for the current input culture.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Suggested replacement words.</returns>
    ValueTask<IReadOnlyList<string>> SuggestAsync(
        string word,
        CultureInfo? culture,
        CancellationToken cancellationToken = default);
}
