using Avalonia.Platform;
using Vortice.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    internal class GlyphRunImpl : IGlyphRunImpl
    {
        public GlyphRunImpl(GlyphRun glyphRun)
        {
            GlyphRun = glyphRun;
        }

        public GlyphRun GlyphRun { get; }

        public void Dispose()
        {
            GlyphRun?.Dispose();
        }
    }
}
