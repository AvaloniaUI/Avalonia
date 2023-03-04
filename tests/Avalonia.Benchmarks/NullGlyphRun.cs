using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Benchmarks
{
    internal class NullGlyphRun : IGlyphRunImpl
    {
        public Rect Bounds => default;

        public Point BaselineOrigin => default;

        public void Dispose()
        {
        }

        public IReadOnlyList<float> GetIntersections(float lowerBound, float upperBound)
        {
            return null;
        }
    }
}
