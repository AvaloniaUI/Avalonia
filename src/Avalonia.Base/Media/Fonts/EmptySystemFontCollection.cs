using System;

namespace Avalonia.Media.Fonts
{
    internal class EmptySystemFontCollection : FontCollectionBase
    {
        public override Uri Key => FontManager.SystemFontsKey;
    }
}
