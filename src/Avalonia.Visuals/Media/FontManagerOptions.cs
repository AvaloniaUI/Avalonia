using System.Collections.Generic;

namespace Avalonia.Media
{
    public class FontManagerOptions
    {
        public string? DefaultFamilyName { get; set; }

        public IReadOnlyList<FontFallback>? FontFallbacks { get; set; }
    }
}
