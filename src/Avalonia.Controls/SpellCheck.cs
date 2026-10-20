using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Input.TextInput;
using Avalonia.Metadata;

namespace Avalonia.Controls;

/// <summary>
/// Configures framework spell checking for a control and its descendants.
/// </summary>
public static class SpellCheck
{
    /// <summary>
    /// Defines the IsEnabled attached property. Inherited; disabled by default.
    /// </summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>(
            "IsEnabled",
            typeof(SpellCheck),
            defaultValue: false,
            inherits: true);

    /// <summary>
    /// Defines the Provider attached property. Inherited; null uses the platform provider of the top level.
    /// </summary>
    [Unstable("Spell checking providers are in early development and may change in minor releases.")]
    public static readonly AttachedProperty<ISpellCheckProvider?> ProviderProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, ISpellCheckProvider?>(
            "Provider",
            typeof(SpellCheck),
            inherits: true);

    /// <summary>
    /// Defines the Languages attached property: BCP 47 language tags such as <c>de-DE</c>. Inherited.
    /// </summary>
    /// <remarks>
    /// In XAML, several tags are separated by commas, for example <c>en-US, es-ES</c>.
    /// With several languages a word is reported only when every language considers it misspelled.
    /// When null or empty, the first valid <see cref="TextInputOptions.LocaleHintsProperty"/> entry is used,
    /// then the preferred application language of the platform, then <see cref="CultureInfo.CurrentUICulture"/>.
    /// Tags that cannot be resolved are skipped with a warning; if none resolve, spell checking is off for the control.
    /// </remarks>
    public static readonly AttachedProperty<IReadOnlyList<string>?> LanguagesProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, IReadOnlyList<string>?>(
            "Languages",
            typeof(SpellCheck),
            inherits: true);

    /// <summary>
    /// Defines the Suggestions attached property. Set by text controls for the error at the context position.
    /// </summary>
    [Unstable("SpellCheck.Suggestions is theme plumbing and may change in minor releases.")]
    public static readonly AttachedProperty<IReadOnlyList<string>> SuggestionsProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, IReadOnlyList<string>>(
            "Suggestions",
            typeof(SpellCheck),
            defaultValue: Array.Empty<string>());

    /// <summary>
    /// Defines the HasSuggestions attached property. Mirrors whether Suggestions is empty.
    /// </summary>
    [Unstable("SpellCheck.HasSuggestions is theme plumbing and may change in minor releases.")]
    public static readonly AttachedProperty<bool> HasSuggestionsProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>(
            "HasSuggestions",
            typeof(SpellCheck));

    static SpellCheck()
    {
        SuggestionsProperty.Changed.AddClassHandler<StyledElement>((element, e) =>
            element.SetValue(HasSuggestionsProperty, e.GetNewValue<IReadOnlyList<string>?>() is { Count: > 0 }));
    }

    public static bool GetIsEnabled(StyledElement element) => element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(StyledElement element, bool value) => element.SetValue(IsEnabledProperty, value);

    [Unstable("Spell checking providers are in early development and may change in minor releases.")]
    public static ISpellCheckProvider? GetProvider(StyledElement element) => element.GetValue(ProviderProperty);

    [Unstable("Spell checking providers are in early development and may change in minor releases.")]
    public static void SetProvider(StyledElement element, ISpellCheckProvider? value) =>
        element.SetValue(ProviderProperty, value);

    public static IReadOnlyList<string>? GetLanguages(StyledElement element) => element.GetValue(LanguagesProperty);

    public static void SetLanguages(StyledElement element, IReadOnlyList<string>? value) =>
        element.SetValue(LanguagesProperty, value);

    [Unstable("SpellCheck.Suggestions is theme plumbing and may change in minor releases.")]
    public static IReadOnlyList<string> GetSuggestions(StyledElement element) =>
        element.GetValue(SuggestionsProperty) ?? Array.Empty<string>();

    [Unstable("SpellCheck.Suggestions is theme plumbing and may change in minor releases.")]
    public static void SetSuggestions(StyledElement element, IReadOnlyList<string>? value)
    {
        if (value is { Count: > 0 })
        {
            element.SetValue(SuggestionsProperty, value);
        }
        else
        {
            element.ClearValue(SuggestionsProperty);
        }
    }

    [Unstable("SpellCheck.HasSuggestions is theme plumbing and may change in minor releases.")]
    public static bool GetHasSuggestions(StyledElement element) => element.GetValue(HasSuggestionsProperty);
}
