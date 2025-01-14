using System.Collections.Generic;

namespace Avalonia.Media
{
    public class FontManagerOptions
    {
        /// <summary>
        /// Gets or sets the default font family's name
        /// </summary>
        public string? DefaultFamilyName { get; set; }

        /// <summary>
        /// Gets or sets the font fallbacks.
        /// </summary>
        /// <remarks>
        /// A fallback is fullfilled before anything else when the font manager tries to match a specific codepoint.
        /// </remarks>
        public IReadOnlyList<FontFallback>? FontFallbacks { get; set; }

        /// <summary>
        /// Gets or sets the font family mappings.
        /// </summary>
        /// <remarks>
        /// A font family mapping is used if a requested family name can't be resolved.
        /// </remarks>
        public IReadOnlyDictionary<string, FontFamily>? FontFamilyMappings { get; set; }
    }
}
