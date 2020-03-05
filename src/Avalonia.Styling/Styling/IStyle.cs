// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;

#nullable enable

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
        /// <param name="target">The control to attach to.</param>
        /// <param name="host">The element that hosts the style.</param>
        /// <returns>
        /// A <see cref="SelectorMatchResult"/> describing how the style matches the control.
        /// </returns>
        SelectorMatchResult TryAttach(IStyleable target, IStyleHost? host);
    }
}
