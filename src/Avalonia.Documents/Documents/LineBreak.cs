// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: LineBreak element. 
//

using System.Windows.Markup; // TrimSurrondingWhitespace

namespace System.Windows.Documents 
{
    /// <summary>
    /// LineBreak element that forces a line breaking. 
    /// </summary>
    [TrimSurroundingWhitespace]
    public class LineBreak : Inline
    {
        /// <summary>
        /// Creates a new LineBreak instance.
        /// </summary>
        public LineBreak()
        {
        }

        /// <summary>
        /// Creates a new LineBreak instance.
        /// </summary>
        /// <param name="insertionPosition">
        /// Optional position at which to insert the new LineBreak. May
        /// be null.
        /// </param>
        public LineBreak(TextPointer insertionPosition)
        {
            if (insertionPosition != null)
            {
                insertionPosition.TextContainer.BeginChange();
            }
            try
            {
                if (insertionPosition != null)
                {
                    // This will throw InvalidOperationException if schema validity is violated.
                    insertionPosition.InsertInline(this);
                }
            }
            finally
            {
                if (insertionPosition != null)
                {
                    insertionPosition.TextContainer.EndChange();
                }
            }
        }
    }
}

