#nullable enable

namespace Avalonia.VisualTree
{
    /// <summary>
    /// Defines an interface through which a <see cref="Visual"/>'s visual parent can be set.
    /// </summary>
    /// <remarks>
    /// You should not usually need to use this interface - it is for advanced scenarios only.
    /// </remarks>
    public interface ISetVisualParent
    {
        /// <summary>
        /// Sets the control's parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void SetParent(IVisual? parent);
    }
}
