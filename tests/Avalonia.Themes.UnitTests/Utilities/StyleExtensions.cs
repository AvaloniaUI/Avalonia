using System.Collections.Generic;
using System.Linq;
using Avalonia.Styling;

namespace Avalonia.Themes.UnitTests.Utilities;

public static class StyleExtensions
{
    public static IEnumerable<StyleBase> EnumerateStyles(this IStyle style)
    {
        if (style is StyleBase styleBase)
        {
            yield return styleBase;
        }

        // IStyle.Children enumerates both Style's nested styles and Styles (collection) children.
        foreach (var nestedStyle in style.Children.SelectMany(s => s.EnumerateStyles()))
        {
            yield return nestedStyle;
        }
    }
}
