namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for tree items and maintains a list of created containers.
    /// </summary>
    public interface ITreeItemContainerGenerator : IItemContainerGenerator
    {
        /// <summary>
        /// Gets the container index for the tree.
        /// </summary>
        TreeContainerIndex? Index { get; }

        /// <summary>
        /// Updates the index based on the parent <see cref="TreeView"/>.
        /// </summary>
        void UpdateIndex();
    }
}
