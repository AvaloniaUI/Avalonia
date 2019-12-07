// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media.Text;

namespace Avalonia.Media
{
    /// <summary>
    /// Holds a hit test result from a <see cref="FormattedText"/>.
    /// </summary>
    public readonly struct TextHitTestResult
    {
        public TextHitTestResult(CharacterHit characterHit, bool isInside, bool isTrailing)
        {
            CharacterHit = characterHit;
            IsInside = isInside;
            IsTrailing = isTrailing;
        }

        /// <summary>
        /// Gets the hit region within the text.
        /// </summary>
        public CharacterHit CharacterHit { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the point is inside the bounds of the text.
        /// </summary>
        public bool IsInside { get; }

        /// <summary>
        /// Gets a value indicating whether the hit is on the trailing edge of the character.
        /// </summary>
        public bool IsTrailing { get; }
    }
}
