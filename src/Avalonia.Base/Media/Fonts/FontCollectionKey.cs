namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Represents a unique key for identifying a font inside a font collection based on style, weight, and stretch attributes.
    /// </summary>
    /// <remarks>Use this key to efficiently look up or group fonts in a collection by their style, weight,
    /// and stretch characteristics.</remarks>
    /// <param name="Style">The font style to use when constructing the key.</param>
    /// <param name="Weight">The font weight to use when constructing the key.</param>
    /// <param name="Stretch">The font stretch to use when constructing the key.</param>
    public readonly record struct FontCollectionKey(FontStyle Style, FontWeight Weight, FontStretch Stretch);
}
