// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Markup.Parsers.CSharp
{
    internal class IdentifierSyntax : ExpressionSyntax
    {
        public IdentifierSyntax(string name)
        {
            Name = name;
        }

        public String Name { get; }
    }
}
