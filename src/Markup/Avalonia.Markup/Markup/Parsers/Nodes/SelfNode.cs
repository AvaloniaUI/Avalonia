using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Data.Core;

namespace Avalonia.Markup.Parsers.Nodes
{
    internal class SelfNode : ExpressionNode
    {
        public override string Description => "$self";
    }
}
