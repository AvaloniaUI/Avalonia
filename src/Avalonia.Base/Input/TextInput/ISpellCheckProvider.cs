using System.Collections.Generic;
using System.Globalization;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput;

/// <summary>
/// Creates spell checking contexts for text views.
/// </summary>
/// <remarks>
/// A custom provider can act as a router: it can inspect the requested culture and delegate to the
/// platform provider or to its own implementation.
/// </remarks>
[Unstable("ISpellCheckProvider is in early development and may change in minor releases.")]
public interface ISpellCheckProvider
{
    /// <summary>
    /// Gets the cultures for which the provider currently has dictionaries.
    /// </summary>
    /// <remarks>
    /// Intended for discovery. The value is a snapshot and may change when platform dictionaries change.
    /// </remarks>
    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    /// <summary>
    /// Creates a context that spell checks one text view in <paramref name="culture"/>.
    /// </summary>
    /// <param name="culture">The culture to check. The provider applies its own fallback, for example from <c>en-GB</c> to <c>en</c>.</param>
    /// <returns>A new context owned by the caller, or <c>null</c> when the culture is not supported.</returns>
    ISpellCheckContext? CreateContext(CultureInfo culture);
}
