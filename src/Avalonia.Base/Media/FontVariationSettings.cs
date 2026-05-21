using System.Collections.Generic;
using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents font variation settings, including normalized axis coordinates, optional variation instance index,
    /// color palette selection, and pixel size for bitmap strikes.
    /// </summary>
    /// <remarks>Use this type to specify font rendering parameters for variable fonts, color fonts, and
    /// bitmap strikes. The settings correspond to OpenType font features such as axis variations (fvar/avar), named
    /// instances, color palettes (COLR/CPAL), and bitmap sizes. All properties are immutable and must be set during
    /// initialization.</remarks>
    public sealed record class FontVariationSettings
    {
        /// <summary>
        /// Gets the normalized variation coordinates for each axis, derived from fvar/avar tables.
        /// </summary>
        public required IReadOnlyDictionary<OpenTypeTag, float> NormalizedCoordinates { get; init; }
        
        /// <summary>
        /// Gets the index of a predefined variation instance (optional).
        /// If specified, NormalizedCoordinates represent that instance.
        /// </summary>
        public int? InstanceIndex { get; init; }
        
        /// <summary>
        /// Gets the color palette index for COLR/CPAL.
        /// </summary>
        public int PaletteIndex { get; init; }
        
        /// <summary>
        /// Gets the pixel size for bitmap strikes.
        /// </summary>
        public int PixelSize { get; init; }
    }
}
