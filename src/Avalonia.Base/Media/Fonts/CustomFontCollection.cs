using System;
using System.IO;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public class CustomFontCollection : FontCollectionBase, IFontCollection2
    {
        public CustomFontCollection(Uri key)
        {
            Key = key;
        }

        public override Uri Key { get; }

        public override void Initialize(IFontManagerImpl fontManager)
        {
            // Nothing to do
        }
 
        public bool TryAddGlyphTypeface(Stream stream)
        {
            var fontManager = FontManager.Current?.PlatformImpl;

            if (fontManager == null)
            {
                return false;
            }

            if (!fontManager.TryCreateGlyphTypeface(stream, FontSimulations.None, out var glyphTypeface))
            {
                return false;
            }

            return TryAddGlyphTypeface(glyphTypeface);
        }      
    }
}
