using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents resource provider in a tree.
    /// </summary>
    public interface IResourceNode : IResourceProvider
    {
        /// <summary>
        /// Gets the parent resource node, if any.
        /// </summary>
        IResourceNode ResourceParent { get; }
    }
}
