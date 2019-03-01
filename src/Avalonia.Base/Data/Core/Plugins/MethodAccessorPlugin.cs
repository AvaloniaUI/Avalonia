using System;
using System.Linq;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins
{
    class MethodAccessorPlugin : IPropertyAccessorPlugin
    {
        public bool Match(object obj, string methodName)
            => obj.GetType().GetRuntimeMethods().Any(x => x.Name == methodName);

        public IPropertyAccessor Start(WeakReference reference, string methodName)
        {
            Contract.Requires<ArgumentNullException>(reference != null);
            Contract.Requires<ArgumentNullException>(methodName != null);

            var instance = reference.Target;
            var method = instance.GetType().GetRuntimeMethods().FirstOrDefault(x => x.Name == methodName);

            if (method != null)
            {
                if (method.GetParameters().Length + (method.ReturnType == typeof(void) ? 0 : 1) > 8)
                {
                    var exception = new ArgumentException("Cannot create a binding accessor for a method with more than 8 parameters or more than 7 parameters if it has a non-void return type.", nameof(methodName));
                    return new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
                }

                return new Accessor(reference, method);
            }
            else
            {
                var message = $"Could not find CLR method '{methodName}' on '{instance}'";
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
                
                Value = method.IsStatic ? method.CreateDelegate(PropertyType) : method.CreateDelegate(PropertyType, reference.Target);
            }

            public override Type PropertyType { get; }

            public override object Value { get; }

            public override bool SetValue(object value, BindingPriority priority) => false;

            protected override void SubscribeCore()
            {
                try
                {
                    PublishValue(Value);
                }
                catch { }
            }

            protected override void UnsubscribeCore()
            {
            }
        }
    }
}
