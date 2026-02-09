using Avalonia.Media;

namespace Avalonia.VisualTree
{
    /// <summary>
    /// Provides a clip that should be applied to a visual's children only.
    /// </summary>
    internal interface IVisualWithChildClip
    {
        /// <summary>
        /// Attempts to get a clip for the visual's children in local coordinates.
        /// </summary>
        bool TryGetChildClip(out RoundedRect clip);
    }
}
