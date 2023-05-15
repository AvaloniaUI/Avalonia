using System;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media;

public interface IImmutableGlyphRunReference : IDisposable
{
    internal IRef<IGlyphRunImpl>? GlyphRun { get; }
}

internal class ImmutableGlyphRunReference : IImmutableGlyphRunReference
{
    public ImmutableGlyphRunReference(IRef<IGlyphRunImpl>? glyphRun)
    {
        GlyphRun = glyphRun;
    }

    public IRef<IGlyphRunImpl>? GlyphRun { get; private set; }
    public void Dispose()
    {
        GlyphRun?.Dispose();
        GlyphRun = null;
    }
}