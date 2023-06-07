using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Reactive;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    internal class TaskStreamPlugin<T> : IStreamPlugin
    {
        [RequiresUnreferencedCode(TrimmingMessages.StreamPluginRequiresUnreferencedCodeMessage)]
        public bool Match(WeakReference<object?> reference)
        {
            return reference.TryGetTarget(out var target) && target is Task<T>;
        }

        [RequiresUnreferencedCode(TrimmingMessages.StreamPluginRequiresUnreferencedCodeMessage)]
        public IObservable<object?> Start(WeakReference<object?> reference)
        {
            if(!(reference.TryGetTarget(out var target) && target is Task<T> task))
            {
                return Observable.Empty<object?>();
            }

            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                case TaskStatus.Faulted:
                    return HandleCompleted(task);
                default:
                    var subject = new LightweightSubject<object?>();
                    task.ContinueWith(
                            _ => HandleCompleted(task).Subscribe(subject),
                            TaskScheduler.FromCurrentSynchronizationContext())
                        .ConfigureAwait(false);
                    return subject;
            }
        }
        
        
        private static IObservable<object?> HandleCompleted(Task<T> task)
        {
            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    return Observable.Return((object?)task.Result);
                case TaskStatus.Faulted:
                    return Observable.Return(new BindingNotification(task.Exception!, BindingErrorType.Error));
                default:
                    throw new AvaloniaInternalException("HandleCompleted called for non-completed Task.");
            }
        }
    }
}
