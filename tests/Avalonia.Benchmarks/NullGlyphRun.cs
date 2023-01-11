using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Benchmarks
{
    internal class NullGlyphRun : IGlyphRunImpl
    {
        public void Dispose()
        {
        }

        public IReadOnlyList<float> GetIntersections(float lowerBound, float upperBound)
        {
            return null;
        }
    }
}
