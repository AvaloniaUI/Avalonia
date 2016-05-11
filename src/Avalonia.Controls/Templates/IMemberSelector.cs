// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Selects a member of an object.
    /// </summary>
    public interface IMemberSelector
    {
        /// <summary>
        /// Selects a member of an object.
        /// </summary>
        /// <param name="o">The obeject.</param>
        /// <returns>The selected member.</returns>
        object Select(object o);
    }
}