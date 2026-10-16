namespace Avalonia.Controls;

/// <summary>
/// Represents an object which particpates in <see cref="TextScaling"/>.
/// </summary>
/// <seealso cref="ITextScaler"/>
public interface ITextScaleable
{
    /// <summary>
    /// Called when the active text scaling algorithm for this object changes.
    /// </summary>
    void OnTextScalingChanged();
}
