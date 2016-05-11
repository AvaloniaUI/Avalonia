using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Threading
{
    public class SingleThreadDispatcher : Dispatcher
    {
        class ThreadingInterface : IPlatformThreadingInterface
        {
            private readonly AutoResetEvent _evnt = new AutoResetEvent(false);
            private readonly JobRunner _timerJobRunner;

            public ThreadingInterface()
            {
                _timerJobRunner = new JobRunner(this);
            }

            public void RunLoop(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _evnt.WaitOne();
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    Signaled?.Invoke();
                    _timerJobRunner.RunJobs();
                }
            }

            public IDisposable StartTimer(TimeSpan interval, Action tick)
                => AvaloniaLocator.Current.GetService<IPclPlatformWrapper>().StartSystemTimer(interval,
                    () => _timerJobRunner.Post(tick, DispatcherPriority.Normal));

            public void Signal() => _evnt.Set();
            //TODO: Actually perform a check
            public bool CurrentThreadIsLoopThread => true;

            public event Action Signaled;
        }

        public SingleThreadDispatcher() : base(new ThreadingInterface())
        {
        }

        public static Dispatcher StartNew(CancellationToken token)
        {
            var dispatcher = new SingleThreadDispatcher();
            AvaloniaLocator.Current.GetService<IPclPlatformWrapper>().PostThreadPoolItem(() =>
            {
                dispatcher.MainLoop(token);
            });
            return dispatcher;
        }
    }
}
