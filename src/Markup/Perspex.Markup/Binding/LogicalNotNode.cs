// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Reactive.Linq;

namespace Perspex.Markup.Binding
{
    internal class LogicalNotNode : ExpressionNode
    {
        public LogicalNotNode(ExpressionNode next)
            : base(next)
        {
        }

        public override IDisposable Subscribe(IObserver<ExpressionValue> observer)
        {
            return Next.Select(x => Negate(x)).Subscribe(observer);
        }

        protected override void SubscribeAndUpdate(object target)
        {
            CurrentValue = new ExpressionValue(target);
        }

        protected override void Unsubscribe(object target)
        {
        }

        private ExpressionValue Negate(ExpressionValue v)
        {
            if (v.HasValue)
            {
                try
                {
                    var boolean = Convert.ToBoolean(v.Value, CultureInfo.InvariantCulture);
                    return new ExpressionValue(!boolean);
                }
                catch
                {
                    // TODO: Maybe should log something here.
                }
            }

            return ExpressionValue.None;
        }
    }
}
