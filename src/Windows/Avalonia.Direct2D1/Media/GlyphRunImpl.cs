using System.Collections.Generic;
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
            //SharpDX already handles this.
            //GlyphRun?.Dispose();
        }

        public IReadOnlyList<float> GetIntersections(float lowerBound, float upperBound)
        {
            return null;
        }
    }
}
