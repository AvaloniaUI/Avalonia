using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.Media;

/// <summary>
/// List of font feature settings
/// </summary>
public class FontFeatureCollection : AvaloniaList<FontFeature>
{
    public FontFeatureCollection()
    {
    }

    public FontFeatureCollection(IEnumerable<FontFeature> features) : base(features)
    {
    }
}
