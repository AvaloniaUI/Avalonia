// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Handles binding to <see cref="IObservable{T}"/>s for the '^' stream binding operator.
    /// </summary>
    public class ObservableStreamPlugin : IStreamPlugin
    {
        static MethodInfo observableSelect;

        /// <summary>
        /// Checks whether this plugin handles the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the value.</param>
        /// <returns>True if the plugin can handle the value; otherwise false.</returns>
        public virtual bool Match(WeakReference reference)
        {
            return reference.Target.GetType().GetInterfaces().Any(x =>
              x.IsGenericType &&
              x.GetGenericTypeDefinition() == typeof(IObservable<>));
        }

        /// <summary>
        /// Starts producing output based on the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <returns>
        /// An observable that produces the output for the value.
        /// </returns>
        public virtual IObservable<object> Start(WeakReference reference)
        {
            var target = reference.Target;

            // If the observable returns a reference type then we can cast it.
            if (target is IObservable<object> result)
            {
                return result;
            };

            // If the observable returns a value type then we need to call Observable.Select on it.
            // First get the type of T in `IObservable<T>`.
            var sourceType = reference.Target.GetType().GetInterfaces().First(x =>
                  x.IsGenericType &&
                  x.GetGenericTypeDefinition() == typeof(IObservable<>)).GetGenericArguments()[0];

            // Get the Observable.Select method.
            var select = GetObservableSelect(sourceType);

            // Make a Box<> delegate of the correct type.
            var funcType = typeof(Func<,>).MakeGenericType(sourceType, typeof(object));
            var box = GetType().GetMethod(nameof(Box), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(sourceType)
                .CreateDelegate(funcType);

            // Call Observable.Select(target, box);
            return (IObservable<object>)select.Invoke(
                null,
                new object[] { target, box });
        }

        private static MethodInfo GetObservableSelect(Type source)
        {
            return GetObservableSelect().MakeGenericMethod(source, typeof(object));
        }

        private static MethodInfo GetObservableSelect()
        {
            if (observableSelect == null)
            {
                observableSelect = typeof(Observable).GetRuntimeMethods().First(x =>
                {
                    if (x.Name == nameof(Observable.Select) &&
                        x.ContainsGenericParameters &&
                        x.GetGenericArguments().Length == 2)
                    {
                        var parameters = x.GetParameters();

                        if (parameters.Length == 2 &&
                            parameters[0].ParameterType.IsConstructedGenericType &&
                            parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IObservable<>) &&
                            parameters[1].ParameterType.IsConstructedGenericType &&
                            parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                        {
                            return true;
                        }
                    }

                    return false;
                });
            }

            return observableSelect;
        }

        private static object Box<T>(T value) => (object)value;
    }
}
