using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Avalonia.Reactive;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Handles binding to <see cref="IObservable{T}"/>s for the '^' stream binding operator.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = TrimmingMessages.IgnoreNativeAotSupressWarningMessage)]
    [RequiresUnreferencedCode(TrimmingMessages.StreamPluginRequiresUnreferencedCodeMessage)]
    internal class ObservableStreamPlugin : IStreamPlugin
    {
        private static MethodInfo? s_observableGeneric;
        private static MethodInfo? s_observableSelect;

        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, "Avalonia.Data.Core.Plugins.ObservableStreamPlugin", "Avalonia.Base")]
        public ObservableStreamPlugin()
        {

        }

        /// <summary>
        /// Checks whether this plugin handles the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the value.</param>
        /// <returns>True if the plugin can handle the value; otherwise false.</returns>
        public virtual bool Match(WeakReference<object?> reference)
        {
            reference.TryGetTarget(out var target);

            return target != null && target.GetType().GetInterfaces().Any(x =>
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
        public virtual IObservable<object?> Start(WeakReference<object?> reference)
        {
            if (!reference.TryGetTarget(out var target) || target is null)
                return Observable.Empty<object?>();

            // If the observable returns a reference type then we can cast it.
            if (target is IObservable<object?> result)
            {
                return result;
            }

            // If the observable returns a value type then we need to call Observable.Select on it.
            // First get the type of T in `IObservable<T>`.
            var sourceType = target.GetType().GetInterfaces().First(x =>
                  x.IsGenericType &&
                  x.GetGenericTypeDefinition() == typeof(IObservable<>)).GetGenericArguments()[0];

            // Get the BoxObservable<T> method.
            var select = GetBoxObservable(sourceType);

            // Call BoxObservable(target);
            return (IObservable<object?>)select.Invoke(
                null,
                new[] { target })!;
        }

        [RequiresUnreferencedCode(TrimmingMessages.StreamPluginRequiresUnreferencedCodeMessage)]
        private static MethodInfo GetBoxObservable(Type source)
        {
            return (s_observableGeneric ??= GetBoxObservable()).MakeGenericMethod(source);
        }

        [RequiresUnreferencedCode(TrimmingMessages.StreamPluginRequiresUnreferencedCodeMessage)]
        private static MethodInfo GetBoxObservable()
        {
            return s_observableSelect
               ??= typeof(ObservableStreamPlugin).GetMethod(nameof(BoxObservable), BindingFlags.Static | BindingFlags.NonPublic)
               ?? throw new InvalidOperationException("BoxObservable method was not found.");
        }

        private static IObservable<object?> BoxObservable<T>(IObservable<T> source)
        {
            return source.Select(v => (object?)v);
        }
    }
}
