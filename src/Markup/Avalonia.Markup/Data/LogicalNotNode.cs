// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Data;

namespace Avalonia.Markup.Data
{
    internal class LogicalNotNode : ExpressionNode, ITransformNode
    {
        public override string Description => "!";

        protected override void NextValueChanged(object value)
        {
            base.NextValueChanged(Negate(value));
        }

        private static object Negate(object v)
        {
            if (v != AvaloniaProperty.UnsetValue)
            {
                var s = v as string;

                if (s != null)
                {
                    bool result;

                    if (bool.TryParse(s, out result))
                    {
                        return !result;
                    }
                    else
                    {
                        return new BindingNotification(
                            new InvalidCastException($"Unable to convert '{s}' to bool."), 
                            BindingErrorType.Error);
                    }
                }
                else
                {
                    try
                    {
                        var boolean = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                        return !boolean;
                    }
                    catch (InvalidCastException)
                    {
                        // The error message here is "Unable to cast object of type 'System.Object'
                        // to type 'System.IConvertible'" which is kinda useless so provide our own.
                        return new BindingNotification(
                            new InvalidCastException($"Unable to convert '{v}' to bool."),
                            BindingErrorType.Error);
                    }
                    catch (Exception e)
                    {
                        return new BindingNotification(e, BindingErrorType.Error);
                    }
                }
            }

            return AvaloniaProperty.UnsetValue;
        }

        public object Transform(object value)
        {
            var originalType = value.GetType();
            var negated = Negate(value);
            if (negated is BindingNotification)
            {
                return negated;
            }
            return Convert.ChangeType(negated, originalType);
        }
    }
}
