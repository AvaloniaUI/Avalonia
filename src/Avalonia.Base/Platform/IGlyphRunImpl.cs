using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    ///     Actual implementation of a glyph run that stores platform dependent resources.
    /// </summary>
    [Unstable]
    public interface IGlyphRunImpl : IDisposable 
    {
        IReadOnlyList<float> GetIntersections(float lowerBound, float upperBound);
    }
}
