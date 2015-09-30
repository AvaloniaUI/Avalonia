// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Perspex.Markup.Parsers.CSharp
{
    internal class BracketedArgumentListSyntax
    {
        public BracketedArgumentListSyntax(IEnumerable<ExpressionSyntax> arguments)
        {
            Arguments = arguments.ToList();
        }

        public IList<ExpressionSyntax> Arguments { get; }
    }
}
