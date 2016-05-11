// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for <see cref="FormattedText"/>.
    /// </summary>
    public interface IFormattedTextImpl : IDisposable
    {
        /// <summary>
        /// Gets or sets the constraint of the text.
        /// </summary>
        Size Constraint { get; set; }

        /// <summary>
        /// Gets the lines in the text.
        /// </summary>
        /// <returns>
        /// A collection of <see cref="FormattedTextLine"/> objects.
        /// </returns>
        IEnumerable<FormattedTextLine> GetLines();

        /// <summary>
        /// Hit tests a point in the text.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>
        /// A <see cref="TextHitTestResult"/> describing the result of the hit test.
        /// </returns>
        TextHitTestResult HitTestPoint(Point point);

        /// <summary>
        /// Gets the bounds rectangle that the specified character occupies.
        /// </summary>
        /// <param name="index">The index of the character.</param>
        /// <returns>The character bounds.</returns>
        Rect HitTestTextPosition(int index);

        /// <summary>
        /// Gets the bounds rectangles that the specified text range occupies.
        /// </summary>
        /// <param name="index">The index of the first character.</param>
        /// <param name="length">The number of characters in the text range.</param>
        /// <returns>The character bounds.</returns>
        IEnumerable<Rect> HitTestTextRange(int index, int length);

        /// <summary>
        /// Gets the size of the text, taking <see cref="Constraint"/> into account.
        /// </summary>
        /// <returns>The bounds box of the text.</returns>
        Size Measure();

        /// <summary>
        /// Sets the foreground brush for the specified text range.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="startIndex">The start of the text range.</param>
        /// <param name="length">The length of the text range.</param>
        void SetForegroundBrush(IBrush brush, int startIndex, int length);
    }
}
