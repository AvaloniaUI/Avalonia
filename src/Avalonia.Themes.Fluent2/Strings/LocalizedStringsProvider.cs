using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.Themes.Fluent2.Strings;

/// <summary>
/// Answers the theme's string resource keys with a translation for
/// <see cref="CultureInfo.CurrentUICulture"/>. Returns false for cultures without a
/// translation (and for all other keys), which falls the lookup through to the
/// invariant English values in InvariantResources.xaml.
/// </summary>
internal sealed class LocalizedStringsProvider : ResourceProvider
{
    public override bool HasResources => true;

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (MapKey(key, out var index) is { } table)
        {
            // Walk the culture hierarchy (de-AT -> de -> invariant) to the closest translation.
            for (var culture = CultureInfo.CurrentUICulture;
                 !string.IsNullOrEmpty(culture.Name);
                 culture = culture.Parent)
            {
                if (table.TryGetValue(culture.Name, out var strings))
                {
                    value = strings[index];
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    private static Dictionary<string, string[]>? MapKey(object key, out int index)
    {
        switch (key as string)
        {
            case "StringDatePickerDayText": index = 0; return LocalizedControlStrings.DateTimeFields;
            case "StringDatePickerMonthText": index = 1; return LocalizedControlStrings.DateTimeFields;
            case "StringDatePickerYearText": index = 2; return LocalizedControlStrings.DateTimeFields;
            case "StringTimePickerHourText": index = 3; return LocalizedControlStrings.DateTimeFields;
            case "StringTimePickerMinuteText": index = 4; return LocalizedControlStrings.DateTimeFields;
            case "StringTimePickerSecondText": index = 5; return LocalizedControlStrings.DateTimeFields;
            case "StringTextFlyoutCutText": index = 0; return LocalizedControlStrings.TextCommands;
            case "StringTextFlyoutCopyText": index = 1; return LocalizedControlStrings.TextCommands;
            case "StringTextFlyoutPasteText": index = 2; return LocalizedControlStrings.TextCommands;
            default: index = 0; return null;
        }
    }
}
