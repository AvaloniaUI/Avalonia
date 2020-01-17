namespace Avalonia.Controls
{
    /// <summary>
    /// Defines an interface through which an <see cref="IResourceNode"/>'s parent can be set.
    /// </summary>
    /// <remarks>
    /// You should not usually need to use this interface - it is for internal use only.
    /// </remarks>
    public interface ISetResourceParent : IResourceNode
    {
        /// <summary>
        /// Sets the resource parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        void SetParent(IResourceNode parent);

        /// <summary>
        /// Notifies the resource node that a change has been made to the resources in its parent.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// This method will be called automatically by the framework, you should not need to call
        /// this method yourself.
        /// </remarks>
        void ParentResourcesChanged(ResourcesChangedEventArgs e);
    }
}
