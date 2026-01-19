using System;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;

namespace Avalonia.Media;

public abstract class CacheMode : StyledElement
{
    // We currently only allow visual to be attached to one compositor at a time, so keep it simple for now
    internal abstract CompositionCacheMode GetForCompositor(Compositor c);

    public static CacheMode Parse(string s)
    {
        if(s == "BitmapCache")
            return new BitmapCache();
        throw new ArgumentException("Unknown CacheMode: " + s);
    }
}