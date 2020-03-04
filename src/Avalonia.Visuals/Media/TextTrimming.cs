// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Media
{
    /// <summary>
    /// Describes how text is trimmed when it overflows.
    /// </summary>
    public enum TextTrimming
    {
        /// <summary>
        /// Text is not trimmed.
        /// </summary>
        None,

        /// <summary>
        /// Text is trimmed at a character boundary. An ellipsis (...) is drawn in place of remaining text.
        /// </summary>
        CharacterEllipsis,

        /// <summary>
        /// Text is trimmed at a word boundary. An ellipsis (...) is drawn in place of remaining text.
        /// </summary>
        WordEllipsis
    }
}
