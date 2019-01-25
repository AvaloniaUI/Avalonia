// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Documents
{
    /// <summary>
    /// Represents a <see cref="TextElement"/> with a <see cref="Text"/> property.
    /// </summary>
    public interface IHasText
    {
        /// <summary>
        /// Gets the element text.
        /// </summary>
        string Text { get; }
    }
}
