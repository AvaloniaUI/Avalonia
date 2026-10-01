using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins
{
    [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TrimmingMessages.ExpressionNodeRequiresDynamicCodeMessage)]
    internal class ReflectionMethodAccessorPlugin : IPropertyAccessorPlugin
    {
        private readonly Dictionary<(Type, string), MethodLookupResult> _methodLookup = new();

        public bool Match(object obj, string methodName) => GetMethod(obj.GetType(), methodName).IsMatch;

        public IPropertyAccessor? Start(WeakReference<object?> reference, string methodName)
        {
            _ = reference ?? throw new ArgumentNullException(nameof(reference));
            _ = methodName ?? throw new ArgumentNullException(nameof(methodName));

            if (!reference.TryGetTarget(out var instance) || instance is null)
                return null;

            var result = GetMethod(instance.GetType(), methodName);

            if (result.Method is { } method)
            {
                return new Accessor(reference, method);
            }
            else
            {
                Exception exception = result.Error is { } error
                    ? new AmbiguousMatchException(error)
                    : new MissingMemberException($"Could not find CLR method '{methodName}' on '{instance}'");
                return new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }
        }

        private MethodLookupResult GetMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type, string methodName)
        {
            var key = (type, methodName);

            if (!_methodLookup.TryGetValue(key, out var result))
            {
                result = FindBestCommandMethod(type, methodName);
                _methodLookup.Add(key, result);
            }

            return result;
        }

        /// <summary>
        /// Finds the method named <paramref name="methodName"/> which can be bound to a command.
        /// </summary>
        /// <remarks>
        /// Priority:
        ///  1. One parameter method
        ///    1a. Object parameter (amongst several overloads)
        ///    1b. Single method with one parameter
        ///  2. Zero parameters method
        /// </remarks>
        private static MethodLookupResult FindBestCommandMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type, string methodName)
        {
            const BindingFlags bindingFlags =
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

            List<MethodInfo>? candidates = null;

            foreach (var methodInfo in type.GetMethods(bindingFlags))
            {
                if (methodInfo.Name == methodName)
                    (candidates ??= []).Add(methodInfo);
            }

            if (candidates is null)
                return default;

            MethodInfo? zeroParamCandidate = null;
            Dictionary<Type, MethodInfo>? oneParamCandidates = null;

            foreach (var candidate in candidates)
            {
                var parameters = candidate.GetParameters();

                switch (parameters.Length)
                {
                    case 0:
                        zeroParamCandidate = GetMostDerived(zeroParamCandidate, candidate);
                        break;

                    case 1:
                        // Reflection can return several methods with the same parameter type when one hides another:
                        // only keep the most derived one, so that overridden or hidden methods are handled properly.
                        var parameterType = parameters[0].ParameterType;
                        oneParamCandidates ??= new Dictionary<Type, MethodInfo>();
                        oneParamCandidates[parameterType] = GetMostDerived(oneParamCandidates.GetValueOrDefault(parameterType), candidate);
                        break;
                }
            }

            if (oneParamCandidates is not null)
            {
                // Object parameter always wins
                if (oneParamCandidates.TryGetValue(typeof(object), out var objectParamCandidate))
                    return new MethodLookupResult(objectParamCandidate, null);

                if (oneParamCandidates.Count == 1)
                    return new MethodLookupResult(oneParamCandidates.Values.First(), null);

                var parameterTypes = oneParamCandidates.Keys
                    .Select(t => $"'{t.FullName}'")
                    .OrderBy(s => s, StringComparer.Ordinal)
                    .ToArray();

                return new MethodLookupResult(
                    null,
                    $"Unable to resolve method of name '{methodName}' on type '{type.FullName}'. " +
                    $"Found {parameterTypes.Length} overloads accepting one parameter: {string.Join(", ", parameterTypes)}. " +
                    "Expected either a single overload with one parameter, or an overload accepting System.Object.");
            }

            if (zeroParamCandidate is { } found)
                return new MethodLookupResult(found, null);

            return new MethodLookupResult(
                null,
                $"Unable to resolve method of name '{methodName}' on type '{type.FullName}'. " +
                $"Found {candidates.Count} overloads accepting more than one parameter. " +
                "Expected a method with zero or one parameter.");
        }

        private static MethodInfo GetMostDerived(MethodInfo? existing, MethodInfo candidate)
        {
            if (existing is null)
                return candidate;

            return existing.DeclaringType is { } existingType &&
                   candidate.DeclaringType is { } candidateType &&
                   existingType != candidateType && existingType.IsAssignableFrom(candidateType) ?
                candidate :
                existing;
        }

        private readonly struct MethodLookupResult(MethodInfo? method, string? error)
        {
            public MethodInfo? Method { get; } = method;
            public string? Error { get; } = error;
            public bool IsMatch => Method is not null || Error is not null;
        }

        [RequiresDynamicCode(TrimmingMessages.ExpressionNodeRequiresDynamicCodeMessage)]
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
                    var parameter = parameters[i];

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
