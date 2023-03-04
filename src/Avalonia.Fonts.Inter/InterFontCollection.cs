using System;
using Avalonia.Media.Fonts;

namespace Avalonia.Fonts.Inter
{
    public sealed class InterFontCollection : EmbeddedFontCollection
    {
        public InterFontCollection() : base(
            new Uri("fonts:Inter", UriKind.Absolute), 
            new Uri("avares://Avalonia.Fonts.Inter/Assets", UriKind.Absolute))
        {
        }
    }
}
