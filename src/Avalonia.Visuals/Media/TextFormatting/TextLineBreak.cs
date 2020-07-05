using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting
{
    public class TextLineBreak
    {
        public TextLineBreak(IReadOnlyList<ShapedTextCharacters> remainingCharacters)
        {
            RemainingCharacters = remainingCharacters;
        }

        /// <summary>
        /// Get the remaining shaped characters that were split up by the <see cref="TextFormatter"/> during the formatting process.
        /// </summary>
        public IReadOnlyList<ShapedTextCharacters> RemainingCharacters { get; }
    }
}
