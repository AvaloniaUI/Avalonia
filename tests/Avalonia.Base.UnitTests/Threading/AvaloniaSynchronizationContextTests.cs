using System;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Threading
{
    public class AvaloniaSynchronizationContextTests
    {
        class ThreadingInterface : IPlatformThreadingInterface
        {
            readonly int threadId;

            public ThreadingInterface()
            {
                threadId = Thread.CurrentThread.ManagedThreadId;
            }

            public bool CurrentThreadIsLoopThread => threadId == Thread.CurrentThread.ManagedThreadId;

            public event Action<DispatcherPriority?> Signaled;

            public void RunLoop(CancellationToken cancellationToken)
            {

            }

            public void Signal(DispatcherPriority priority) => 
                Signaled?.Invoke(priority);

            public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick) => 
                System.Reactive.Disposables.Disposable.Empty;
        }

        [Fact]
        public async void Unwrap_Exception_When_Call_Send_Not_In_Current_Context()
        {
            using (UnitTestApplication.Start(new TestServices(threadingInterface: new ThreadingInterface())))
            {
                AvaloniaSynchronizationContext.InstallIfNeeded();
                var ctx = SynchronizationContext.Current;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                _ = await Assert.ThrowsAsync<ArgumentException>(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                    {
                          ctx.Send(state => throw new ArgumentException("hello"), null);
                    });
            }
        }
    }
}
