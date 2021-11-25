#nullable enable

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for items in an <see cref="ItemsControl"/>.
    /// </summary>
    public interface IItemContainerGenerator
    {
        /// <summary>
        /// Realizes a container for the specified item.
        /// </summary>
        /// <param name="parent">The parent control that will host the container.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="item">The item.</param>
        /// <returns>The realized control.</returns>
        /// <remarks>
        /// May create a new control or return a control from a recycle pool. The returned control's
        /// logical and visual parent must be <paramref name="parent"/> or null.
        /// </remarks>
        IControl Realize(IControl parent, int index, object item);

        /// <summary>
        /// Unrealizes a container for the specified item.
        /// </summary>
        /// <param name="container">The container to be unrealized.</param>
        /// <param name="index">The index of the item.</param>
        /// <param name="item">The item.</param>
        /// <remarks>
        /// On entry to this method, the container's logical and visual parent will be set. This
        /// method may remove the container from its parent or recycle it in-place by making it
        /// invisible in some manner.
        /// </remarks>
        void Unrealize(IControl container, int index, object item);
    }
}
