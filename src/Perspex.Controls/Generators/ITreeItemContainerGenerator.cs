// -----------------------------------------------------------------------
// <copyright file="ITreeItemContainerGenerator.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Generators
{
    using System.Collections.Generic;

    /// <summary>
    /// Creates containers for tree items and maintains a list of created containers.
    /// </summary>
    public interface ITreeItemContainerGenerator : IItemContainerGenerator
    {
        /// <summary>
        /// Gets all of the generated container controls.
        /// </summary>
        /// <returns>The containers.</returns>
        IEnumerable<IControl> GetAllContainers();

        /// <summary>
        /// Gets the item that is contained by the specified container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The item.</returns>
        object ItemFromContainer(IControl container);

        /// <summary>
        /// Gets the container for the specified item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The container.</returns>
        IControl ContainerFromItem(object item);
    }
}