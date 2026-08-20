using System;

namespace Avalonia;

/// <summary>
/// Represents an object which can algoritmically scale text.
/// </summary>
public interface ITextScaler
{
    double GetScaledFontSize(Visual target, double baseFontSize);

    /// <summary>
    /// Raised when the text scaling algorithm has changed. Indicates that all text should be rescaled.
    /// </summary>
    event EventHandler<EventArgs>? TextScalingChanged;
}
