using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public interface IFontCollection : IReadOnlyList<FontFamily>, IDisposable
    {
        Uri Key { get; }

        void Initialize(IFontManagerImpl fontManager);

        bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface);
    }
}
