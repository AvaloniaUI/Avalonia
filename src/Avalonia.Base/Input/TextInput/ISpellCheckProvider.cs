using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput;

/// <summary>
/// Provides spell-checking services for text input controls.
/// </summary>
/// <remarks>
/// Called on the UI thread. A null culture lets the provider choose the language.
/// </remarks>
[Unstable("ISpellCheckProvider is in early development and may change in minor releases.")]
public interface ISpellCheckProvider
{
    /// <summary>
    /// Gets a value indicating whether spell checking is available for the specified culture.
    /// </summary>
    /// <param name="culture">The culture to check, or null to let the provider choose the language.</param>
    /// <returns>True when spell checking is available; otherwise false.</returns>
    bool IsLanguageSupported(CultureInfo? culture);

    /// <summary>
    /// Checks text and returns the misspelled ranges.
    /// </summary>
    /// <param name="text">
    /// The text to check, valid until the returned task completes.
    /// </param>
    /// <param name="culture">The culture to check, or null to let the provider choose the language.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Misspelled ranges with offsets relative to <paramref name="text"/>.
    /// </returns>
    ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
        ReadOnlyMemory<char> text,
        CultureInfo? culture,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets replacement suggestions for a misspelled word.
    /// </summary>
    /// <param name="word">The misspelled word.</param>
    /// <param name="culture">The culture to check, or null to let the provider choose the language.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Suggested replacement words.</returns>
    ValueTask<IReadOnlyList<string>> SuggestAsync(
        string word,
        CultureInfo? culture,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Identifies dictionary changes that leave the requested culture unchanged.
/// </summary>
internal interface ISpellCheckProviderWithLanguageIdentity
{
    /// <summary>
    /// Returns the current dictionary's stable identity, or null if unavailable.
    /// </summary>
    string? GetLanguageIdentity(CultureInfo? culture);
}
