using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Data;
using System.Reflection;
using System.Linq;

namespace Avalonia.Markup.Data.Plugins
{
    class MethodAccessorPlugin : IPropertyAccessorPlugin
    {
        public bool Match(object obj, string propertyName)
            => obj.GetType().GetRuntimeMethods().FirstOrDefault(x => x.Name == propertyName) != null;

        public IPropertyAccessor Start(WeakReference reference, string propertyName)
        {
            Contract.Requires<ArgumentNullException>(reference != null);
            Contract.Requires<ArgumentNullException>(propertyName != null);

            var instance = reference.Target;
            var method = instance.GetType().GetRuntimeMethods().FirstOrDefault(x => x.Name == propertyName);

            if (method != null)
            {
                return new Accessor(reference, method);
            }
            else
            {
                var message = $"Could not find CLR method '{propertyName}' on '{instance}'";
                var exception = new MissingMemberException(message);
                return new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }
        }

        private class Accessor : PropertyAccessorBase
        {
            public Accessor(WeakReference reference, MethodInfo method)
            {
                Contract.Requires<ArgumentNullException>(reference != null);
                Contract.Requires<ArgumentNullException>(method != null);

                var paramTypes = method.GetParameters().Select(param => param.ParameterType).ToArray();
                var returnType = method.ReturnType;

                // TODO: Throw exception if more than 8 parameters or more than 7 + return type.
                // Do this here or in the caller? Here probably
                if (returnType == typeof(void))
                {
                    if (paramTypes.Length == 0)
                    {
                        PropertyType = typeof(Action);
                    }
                    else
                    {
                        PropertyType = Type.GetType($"System.Action`{paramTypes.Length}").MakeGenericType(paramTypes); 
                    }
                }
                else
                {
                    var genericTypeParameters = paramTypes.Concat(new[] { returnType }).ToArray();
                    PropertyType = Type.GetType($"System.Func`{genericTypeParameters.Length}").MakeGenericType(genericTypeParameters);
                }

                // TODO: Is this going to leak?
                // TODO: Static methods?
                Value = method.CreateDelegate(PropertyType, reference.Target);
            }

            public override Type PropertyType { get; }

            public override object Value { get; }

            public override bool SetValue(object value, BindingPriority priority) => false;

            protected override void SubscribeCore(IObserver<object> observer)
            {
                SendCurrentValue();
            }

            private void SendCurrentValue()
            {
                try
                {
                    var value = Value;
                    Observer.OnNext(value);
                }
                catch { }
            }
        }
    }
}
