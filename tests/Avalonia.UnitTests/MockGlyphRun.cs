using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockGlyphRun : IGlyphRunImpl
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
