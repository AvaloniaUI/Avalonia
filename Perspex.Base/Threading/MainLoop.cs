// -----------------------------------------------------------------------
// <copyright file="MainLoop.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Win32.Threading
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NGenerics.DataStructures.Queues;
    using Perspex.Platform;
    using Perspex.Threading;
    using Splat;

    internal class MainLoop
    {
        private static IPlatformThreadingInterface platform;

        private PriorityQueue<Job, DispatcherPriority> queue =
            new PriorityQueue<Job, DispatcherPriority>(PriorityQueueType.Maximum);

        static MainLoop()
        {
            platform = Locator.Current.GetService<IPlatformThreadingInterface>();
        }

        public void Run(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Job job;

                // TODO: Dispatch windows messages in preference to lower priority jobs.
                while (this.queue.Count > 0)
                {
                    lock (this.queue)
                    {
                        job = this.queue.Dequeue();
                    }

                    try
                    {
                        job.Action();
                        job.TaskCompletionSource.SetResult(null);
                    }
                    catch (Exception e)
                    {
                        job.TaskCompletionSource.SetException(e);
                    }
                }

                platform.ProcessMessage();
            }
        }

        public Task InvokeAsync(Action action, DispatcherPriority priority)
        {
            var job = new Job(action);

            lock (this.queue)
            {
                this.queue.Add(job, priority);
            }

            platform.Wake();
            return job.TaskCompletionSource.Task;
        }

        private class Job
        {
            public Job(Action action)
            {
                this.Action = action;
                this.TaskCompletionSource = new TaskCompletionSource<object>();
            }

            public Action Action { get; private set; }

            public TaskCompletionSource<object> TaskCompletionSource { get; set; }
        }
    }
}
