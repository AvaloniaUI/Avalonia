using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockGlyphRun : IGlyphRunImpl
    {
        public MockGlyphRun(IReadOnlyList<GlyphInfo> glyphInfos)
        {
            Size = new Size(glyphInfos.Sum(x=> x.GlyphAdvance), 10);
        }

        public Size Size { get; }

        public Point BaselineOrigin => new Point(0, 8);

        public void Dispose()
        {
            
        }

        public IReadOnlyList<float> GetIntersections(float lowerBound, float upperBound)
        {
            return null;
        }
    }
}
