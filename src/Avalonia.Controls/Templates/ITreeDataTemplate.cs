// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Interface representing a template used to build hierachical data.
    /// </summary>
    public interface ITreeDataTemplate : IDataTemplate
    {
        /// <summary>
        /// Selects the child items of an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        /// An <see cref="InstancedBinding"/> holding the items, or an observable that tracks the
        /// items. May return null if no child items.
        /// </returns>
        InstancedBinding ItemsSelector(object item);
    }
}