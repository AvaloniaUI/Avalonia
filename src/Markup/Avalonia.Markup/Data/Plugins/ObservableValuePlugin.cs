// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Data;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// Handles binding to <see cref="IObservable{T}"/>s in an <see cref="ExpressionObserver"/>.
    /// </summary>
    public class ObservableValuePlugin : IValuePlugin
    {
        /// <summary>
        /// Checks whether this plugin handles the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the value.</param>
        /// <returns>True if the plugin can handle the value; otherwise false.</returns>
        public virtual bool Match(WeakReference reference)
        {
            var target = reference.Target;

            // ReactiveCommand is an IObservable but we want to bind to it, not its value.
            return target is IObservable<object> && !(target is ICommand);
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
            return reference.Target as IObservable<object>;
        }
    }
}
