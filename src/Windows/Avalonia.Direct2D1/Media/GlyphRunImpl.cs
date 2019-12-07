using Avalonia.Platform;

namespace Avalonia.Direct2D1.Media
{
    internal class GlyphRunImpl : IGlyphRunImpl
    {
        public GlyphRunImpl(SharpDX.DirectWrite.GlyphRun glyphRun)
        {
            GlyphRun = glyphRun;
        }

        public SharpDX.DirectWrite.GlyphRun GlyphRun { get; }

        public void Dispose()
        {
            GlyphRun?.Dispose();
        }
    }
}
