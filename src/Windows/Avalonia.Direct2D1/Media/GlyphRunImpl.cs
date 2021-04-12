using Avalonia.Platform;

namespace Avalonia.Direct2D1.Media
{
    internal class GlyphRunImpl : IGlyphRunImpl
    {
        public GlyphRunImpl(Vortice.DirectWrite.GlyphRun glyphRun)
        {
            GlyphRun = glyphRun;
        }

        public Vortice.DirectWrite.GlyphRun GlyphRun { get; }

        public void Dispose()
        {
            GlyphRun?.Dispose();
        }
    }
}
