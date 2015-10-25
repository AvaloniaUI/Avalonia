// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Markup.Data.Parsers;

namespace Perspex.Markup.Data
{
    public class ExpressionParseException : Exception
    {
        internal ExpressionParseException(int column, string message)
            : base(message)
        {
            Column = column;
        }

        internal ExpressionParseException(Reader r, string message)
            : this(r.Position, message)
        {
        }

        public int Column { get; }
    }
}
