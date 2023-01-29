using System;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Represents a single layout pass timing.
    /// </summary>
    /// <param name="PassCounter">The number of the layout pass.</param>
    /// <param name="Elapsed">The elapsed time during the layout pass.</param>
    public readonly record struct LayoutPassTiming(int PassCounter, TimeSpan Elapsed);
}
