using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Threading;

public partial class Dispatcher
{
    private TaskScheduler? _taskScheduler;

    /// <summary>
    /// Gets a <see cref="TaskScheduler"/> which executes tasks on this <see cref="Dispatcher"/>. A <see cref="DispatcherPriority"/> is captured from
    /// the current <see cref="AvaloniaSynchronizationContext"/> if one is available. Otherwise, <see cref="DispatcherPriority.Default"/> is used.
    /// </summary>
    public TaskScheduler ToTaskScheduler() => ToTaskScheduler(SynchronizationContext.Current switch
    {
        AvaloniaSynchronizationContext avaloniaContext => avaloniaContext.Priority,
        _ => DispatcherPriority.Default
    });

    /// <summary>
    /// Gets a <see cref="TaskScheduler"/> which executes tasks on this <see cref="Dispatcher"/> with the specified <see cref="DispatcherPriority"/>.
    /// </summary>
    public TaskScheduler ToTaskScheduler(DispatcherPriority priority)
    {
        lock (InstanceLock)
        {
            if (_taskScheduler == null)
            {
                var prevContext = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(GetContextWithPriority(priority));
                try
                {
                    _taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(prevContext);
                }
            }
        }

        return _taskScheduler;
    }
}
