namespace Avalonia.Media;

/// <summary>
/// Specifies which bounds of a visual its backdrop effect is limited to.
/// </summary>
public enum BackdropEffectBoundsSource
{
    /// <summary>
    /// The layout bounds of the visual.
    /// </summary>
    Layout,

    /// <summary>
    /// The bounds of the visual's subtree.
    /// </summary>
    Subtree
}
