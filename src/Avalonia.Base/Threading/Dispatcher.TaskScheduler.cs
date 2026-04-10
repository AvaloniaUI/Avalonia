using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Threading;

public partial class Dispatcher
{
    private TaskScheduler? _taskScheduler;

    public static implicit operator TaskScheduler(Dispatcher dispatcher)
    {
        lock (dispatcher.InstanceLock)
        {
            if (dispatcher._taskScheduler == null)
            {
                var prevContext = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(dispatcher.GetContextWithPriority(DispatcherPriority.Default));
                try
                {
                    dispatcher._taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(prevContext);
                }
            }
        }

        return dispatcher._taskScheduler;
    }
}
