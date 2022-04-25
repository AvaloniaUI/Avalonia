﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins
{
    public class MethodAccessorPlugin : IPropertyAccessorPlugin
    {
        private readonly Dictionary<(Type, string), MethodInfo?> _methodLookup =
            new Dictionary<(Type, string), MethodInfo?>();

        public bool Match(object obj, string methodName) => GetFirstMethodWithName(obj.GetType(), methodName) != null;

        public IPropertyAccessor? Start(WeakReference<object?> reference, string methodName)
        {
            _ = reference ?? throw new ArgumentNullException(nameof(reference));
            _ = methodName ?? throw new ArgumentNullException(nameof(methodName));

            if (!reference.TryGetTarget(out var instance) || instance is null)
                return null;

            var method = GetFirstMethodWithName(instance.GetType(), methodName);

            if (method is not null)
            {
                return new Accessor(reference, method);
            }
            else
            {
                var message = $"Could not find CLR method '{methodName}' on '{instance}'";
                var exception = new MissingMemberException(message);
                return new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }
        }

        private MethodInfo? GetFirstMethodWithName(Type type, string methodName)
        {
            var key = (type, methodName);

            if (!_methodLookup.TryGetValue(key, out var methodInfo))
            {
                methodInfo = TryFindAndCacheMethod(type, methodName);
            }

            return methodInfo;
        }

        private MethodInfo? TryFindAndCacheMethod(Type type, string methodName)
        {
            MethodInfo? found = null;

            const BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

            var methods = type.GetMethods(bindingFlags);

            foreach (MethodInfo methodInfo in methods)
            {
                if (methodInfo.Name == methodName)
                {
                    found = methodInfo;

                    break;
                }
            }

            _methodLookup.Add((type, methodName), found);

            return found;
        }

        private sealed class Accessor : PropertyAccessorBase
        {
            public Accessor(WeakReference<object?> reference, MethodInfo method)
            {
                _ = reference ?? throw new ArgumentNullException(nameof(reference));
                _ = method ?? throw new ArgumentNullException(nameof(method));

                var returnType = method.ReturnType;

                var parameters = method.GetParameters();

                var signatureTypeCount = parameters.Length + 1;

                var paramTypes = new Type[signatureTypeCount];


                for (var i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo parameter = parameters[i];

                    paramTypes[i] = parameter.ParameterType;
                }

                paramTypes[paramTypes.Length - 1] = returnType;

                PropertyType = Expression.GetDelegateType(paramTypes);

                if (method.IsStatic)
                {
                    Value = method.CreateDelegate(PropertyType);
                }
                else if (reference.TryGetTarget(out var target))
                {
                    Value = method.CreateDelegate(PropertyType, target);
                }
            }

            public override Type? PropertyType { get; }

            public override object? Value { get; }

            public override bool SetValue(object? value, BindingPriority priority) => false;

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
