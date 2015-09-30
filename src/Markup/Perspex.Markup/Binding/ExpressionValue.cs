// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Markup.Binding
{
    public class ExpressionValue
    {
        public static readonly ExpressionValue None = new ExpressionValue();

        public ExpressionValue(object value)
        {
            HasValue = true;
            Value = value;
        }

        private ExpressionValue()
        {
            HasValue = false;
        }

        public bool HasValue { get; }
        public object Value { get; }
    }
}
