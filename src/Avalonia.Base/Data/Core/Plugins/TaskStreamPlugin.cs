// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Handles binding to <see cref="Task"/>s for the '^' stream binding operator.
    /// </summary>
    public class TaskStreamPlugin : IStreamPlugin
    {
        /// <summary>
        /// Checks whether this plugin handles the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the value.</param>
        /// <returns>True if the plugin can handle the value; otherwise false.</returns>
        public virtual bool Match(WeakReference<object> reference)
        {
            reference.TryGetTarget(out object target);

            return target is Task;
        } 

        /// <summary>
        /// Starts producing output based on the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <returns>
        /// An observable that produces the output for the value.
        /// </returns>
        public virtual IObservable<object> Start(WeakReference<object> reference)
        {
            reference.TryGetTarget(out object target);

            if (target is Task task)
            {
                var resultProperty = task.GetType().GetRuntimeProperty("Result");

                if (resultProperty != null)
                {
                    switch (task.Status)
                    {
                        case TaskStatus.RanToCompletion:
                        case TaskStatus.Faulted:
                            return HandleCompleted(task);
                        default:
                            var subject = new Subject<object>();
                            task.ContinueWith(
                                    x => HandleCompleted(task).Subscribe(subject),
                                    TaskScheduler.FromCurrentSynchronizationContext())
                                .ConfigureAwait(false);
                            return subject;
                    }
                }
            }

            return Observable.Empty<object>();
        }

        protected IObservable<object> HandleCompleted(Task task)
        {
            var resultProperty = task.GetType().GetRuntimeProperty("Result");
            
            if (resultProperty != null)
            {
                switch (task.Status)
                {
                    case TaskStatus.RanToCompletion:
                        return Observable.Return(resultProperty.GetValue(task));
                    case TaskStatus.Faulted:
                        return Observable.Return(new BindingNotification(task.Exception, BindingErrorType.Error));
                    default:
                        throw new AvaloniaInternalException("HandleCompleted called for non-completed Task.");
                }
            }

            return Observable.Empty<object>();
        }
    }
}
