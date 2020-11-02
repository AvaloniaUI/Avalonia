using System;

namespace MicroComGenerator
{
    class ParseException : Exception
    {
        public int Line { get; }
        public int Position { get; }

        public ParseException(string message, int line, int position) : base(message)
        {
            Line = line;
            Position = position;
        }

        public ParseException(string message, ref TokenParser parser) : this(message, parser.Line, parser.Position)
        {
        }
    }

    class CodeGenException : Exception
    {
        public CodeGenException(string message) : base(message)
        {
        }
    }
}
