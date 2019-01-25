// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Metadata;

namespace Avalonia.Documents
{
    /// <summary>
    /// Represents a line break.
    /// </summary>
    [TrimSurroundingWhitespace]
    public class LineBreak : Inline
    {
        /// <inheritdoc/>
        public override void BuildFormattedText(FormattedTextBuilder builder)
        {
            builder.Add(Environment.NewLine, null);
        }
    }
}
