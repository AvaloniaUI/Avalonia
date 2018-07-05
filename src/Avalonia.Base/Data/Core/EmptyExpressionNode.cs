// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;

namespace Avalonia.Data.Core
{
    internal class EmptyExpressionNode : ExpressionNode
    {
        public override string Description => ".";
    }
}
