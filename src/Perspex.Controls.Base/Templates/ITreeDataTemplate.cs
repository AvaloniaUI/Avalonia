// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections;

namespace Perspex.Controls.Templates
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
        /// <returns>The child items, or null if no child items.</returns>
        IEnumerable ItemsSelector(object item);
    }
}