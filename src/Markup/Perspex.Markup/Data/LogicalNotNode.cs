// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Reactive.Linq;

namespace Perspex.Markup.Data
{
    internal class LogicalNotNode : ExpressionNode
    {
        public override bool SetValue(object value)
        {
            throw new NotSupportedException("Cannot set a negated binding.");
        }

        public override IDisposable Subscribe(IObserver<object> observer)
        {
            return Next.Select(Negate).Subscribe(observer);
        }

        private static object Negate(object v)
        {
            if (v != PerspexProperty.UnsetValue)
            {
                var s = v as string;

                if (s != null)
                {
                    bool result;

                    if (bool.TryParse(s, out result))
                    {
                        return !result;
                    }
                }
                else
                {
                    try
                    {
                        var boolean = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                        return !boolean;
                    }
                    catch
                    {
                        // TODO: Maybe should log something here.
                    }
                }
            }

            return PerspexProperty.UnsetValue;
        }
    }
}
