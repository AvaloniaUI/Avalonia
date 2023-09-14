using System;
using System.Collections.Generic;
using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    internal class CompositeFontFamilyKey : FontFamilyKey
    {
        public CompositeFontFamilyKey(Uri source, FontFamilyKey[] keys) : base(source, null)
        {
            Keys = keys;
        }

        public IReadOnlyList<FontFamilyKey> Keys { get; }
    }
}
