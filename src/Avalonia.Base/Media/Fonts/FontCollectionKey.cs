using System;
using System.Collections.Generic;

namespace Avalonia.Media.Fonts
{
    public readonly record struct FontCollectionKey(FontStyle Style, FontWeight Weight, FontStretch Stretch);
}
