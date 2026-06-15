using Avalonia.VisualTree;

namespace Avalonia.Animation
{
    /// <summary>
    /// Describes a single visible page within a carousel viewport.
    /// </summary>
    public readonly record struct PageTransitionItem(
        int Index,
        Visual Visual,
        double ViewportCenterOffset);
}
