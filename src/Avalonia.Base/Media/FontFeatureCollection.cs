using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Utilities;

namespace Avalonia.Media;

/// <summary>
/// List of font feature settings
/// </summary>
public class FontFeatureCollection : AvaloniaList<FontFeature>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FontFeatureCollection"/>.
    /// </summary>
    public FontFeatureCollection()
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontFeatureCollection"/> that is empty and has the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The number of font features that the new collection can initially store.</param>
    public FontFeatureCollection(int capacity) : base(capacity)
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FontFeatureCollection"/> that contains font features copied from the specified collection.
    /// </summary>
    /// <param name="fontFeatures">The collection whose font features are copied to the new collection.</param>
    public FontFeatureCollection(IEnumerable<FontFeature> fontFeatures) : base(fontFeatures)
    {

    }

    /// <summary>
    /// Parses a <see cref="FontFeatureCollection"/> string.
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>The <see cref="FontFeatureCollection"/>.</returns>
    public static FontFeatureCollection Parse(string s)
    {
        var features = new List<FontFeature>();

        using (var tokenizer = new SpanStringTokenizer(s, ',', "Invalid font feature specification."))
        {
            while (tokenizer.TryReadSpan(out var token))
            {
                FontFeature feature = FontFeature.Parse(token.ToString());
                features.Add(feature);
            }
        }

        return new FontFeatureCollection(features);
    }
}
