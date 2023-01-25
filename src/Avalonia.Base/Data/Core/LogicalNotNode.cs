using System;
using System.Globalization;

namespace Avalonia.Data.Core
{
    internal class LogicalNotNode : ExpressionNode, ITransformNode
    {
        public override string Description => "!";

        protected override void NextValueChanged(object? value)
        {
            base.NextValueChanged(Negate(value));
        }

        private static object Negate(object? value)
        {
            var notification = value as BindingNotification;
            var v = BindingNotification.ExtractValue(value);

            BindingNotification GenerateError(Exception e)
            {
                notification ??= new BindingNotification(AvaloniaProperty.UnsetValue);
                notification.AddError(e, BindingErrorType.Error);
                notification.ClearValue();
                return notification;
            }

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
                        return GenerateError(new InvalidCastException($"Unable to convert '{s}' to bool."));
                    }
                }
                else
                {
                    try
                    {
                        var boolean = Convert.ToBoolean(v, CultureInfo.InvariantCulture);

                        if (notification is object)
                        {
                            notification.SetValue(!boolean);
                            return notification;
                        }
                        else
                        {
                            return !boolean;
                        }
                    }
                    catch (InvalidCastException)
                    {
                        // The error message here is "Unable to cast object of type 'System.Object'
                        // to type 'System.IConvertible'" which is kinda useless so provide our own.
                        return GenerateError(new InvalidCastException($"Unable to convert '{v}' to bool."));
                    }
                    catch (Exception e)
                    {
                        return GenerateError(e);
                    }
                }
            }

            return notification ?? AvaloniaProperty.UnsetValue;
        }

        public object? Transform(object? value)
        {
            if (value is null)
                return null;

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
