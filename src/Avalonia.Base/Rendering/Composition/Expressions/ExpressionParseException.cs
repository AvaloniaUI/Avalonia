using System;

// Special license applies, see //file: src/Avalonia.Base/Rendering/Composition/License.md

namespace Avalonia.Rendering.Composition.Expressions
{
    internal class ExpressionParseException : Exception
    {
        public int Position { get; }

        public ExpressionParseException(string message, int position) : base(message)
        {
            Position = position;
        }
    }
}