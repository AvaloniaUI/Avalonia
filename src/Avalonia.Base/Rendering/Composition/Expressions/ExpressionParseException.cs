using System;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

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
