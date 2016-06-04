// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Interface representing a template used to build a control for a piece of data.
    /// </summary>
    public interface IDataTemplate : ITemplate<object, IControl>
    {
        /// <summary>
        /// Gets a value indicating whether the data template supports recycling of the generated
        /// control.
        /// </summary>
        bool SupportsRecycling { get; }

        /// <summary>
        /// Checks to see if this data template matches the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>
        /// True if the data template can build a control for the data, otherwise false.
        /// </returns>
        bool Match(object data);
    }
}