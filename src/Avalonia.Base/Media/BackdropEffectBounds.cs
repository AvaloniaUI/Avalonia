namespace Avalonia.Media;

/// <summary>
/// Specifies the bounds used when applying a backdrop effect.
/// </summary>
public enum BackdropEffectBounds
{
    /// <summary>
    /// Uses the visual's layout bounds.
    /// </summary>
    Layout,

    /// <summary>
    /// Uses the bounds of the visual and its descendants.
    /// </summary>
    Subtree
}
