// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Skia.Text
{
    public struct SKTextPointer
    {
        public SKTextPointer(int startingIndex, int length)
        {
            StartingIndex = startingIndex;
            Length = length;
        }

        /// <summary>
        /// Gets the starting index.
        /// </summary>
        /// <value>
        /// The starting index.
        /// </value>
        public int StartingIndex { get; }

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public int Length { get; }

        public override string ToString()
        {
            return $"({StartingIndex}:{Length})";
        }
    }
}
