// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines the interface for styles.
    /// </summary>
    public interface IStyle : IResourceNode
    {
        /// <summary>
        /// Attaches the style to a control if the style's selector matches.
        /// </summary>
        /// <param name="control">The control to attach to.</param>
        /// <param name="container">
        /// The control that contains this style. May be null.
        /// </param>
        void Attach(IStyleable control, IStyleHost container);
    }
}
