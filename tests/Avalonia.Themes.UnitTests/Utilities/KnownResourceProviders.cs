using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

namespace Avalonia.Themes.UnitTests.Utilities;

/// <summary>
/// Fluent theme uses ResourceProviders for computed resources, which don't have any public API to read Keys collections.
/// TODO: revisit Avalonia API, and consider adding IReadOnlyResourceDictionary or KeyedResourceProvider of sorts. 
/// </summary>
internal class KnownResourceProviders
{
    private static readonly string[] s_accentKeys =
    [
        "SystemAccentColor",
        "SystemAccentColorDark1",
        "SystemAccentColorDark2",
        "SystemAccentColorDark3",
        "SystemAccentColorLight1",
        "SystemAccentColorLight2",
        "SystemAccentColorLight3"
    ];

    private static readonly string[] s_colorKeys =
    [
        "SystemAltHighColor",
        "SystemAltLowColor",
        "SystemAltMediumColor",
        "SystemAltMediumHighColor",
        "SystemAltMediumLowColor",
        "SystemBaseHighColor",
        "SystemBaseLowColor",
        "SystemBaseMediumColor",
        "SystemBaseMediumHighColor",
        "SystemBaseMediumLowColor",
        "SystemChromeAltLowColor",
        "SystemChromeBlackHighColor",
        "SystemChromeBlackLowColor",
        "SystemChromeBlackMediumColor",
        "SystemChromeBlackMediumLowColor",
        "SystemChromeDisabledHighColor",
        "SystemChromeDisabledLowColor",
        "SystemChromeGrayColor",
        "SystemChromeHighColor",
        "SystemChromeLowColor",
        "SystemChromeMediumColor",
        "SystemChromeMediumLowColor",
        "SystemChromeWhiteColor",
        "SystemErrorTextColor",
        "SystemListLowColor",
        "SystemListMediumColor",
        "SystemRegionColor"
    ];

    public static IEnumerable<string>? TryGetKnownResourceKeys(IResourceProvider provider)
    {
        if (provider.GetType().Name == "SystemAccentColors")
        {
            return s_accentKeys;
        }

        if (provider.GetType().Name == "ColorPaletteResources")
        {
            return s_accentKeys.Concat(s_colorKeys);
        }

        return null;
    }
}
