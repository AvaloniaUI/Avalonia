// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Styling
{
    /// <summary>
    /// Defines the interface for styles.
    /// </summary>
    public interface IStyle
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
