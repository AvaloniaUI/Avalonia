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

    /// <summary>
    /// A main loop in a <see cref="Dispatcher"/>.
    /// </summary>
    internal class MainLoop
    {
        private static IPlatformThreadingInterface platform;

        private PriorityQueue<Job, DispatcherPriority> queue =
            new PriorityQueue<Job, DispatcherPriority>(PriorityQueueType.Maximum);

        /// <summary>
        /// Initializes static members of the <see cref="MainLoop"/> class.
        /// </summary>
        static MainLoop()
        {
            platform = Locator.Current.GetService<IPlatformThreadingInterface>();
        }

        /// <summary>
        /// Runs the main loop.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token used to exit the main loop.
        /// </param>
        public void Run(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Job job = null;

                while (job != null || this.queue.Count > 0)
                {
                    if (job == null)
                    {
                        lock (this.queue)
                        {
                            job = this.queue.Dequeue();
                        }
                    }

                    if (job.Priority < DispatcherPriority.Input && platform.HasMessages())
                    {
                        break;
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

                    job = null;
                }

                platform.ProcessMessage();
            }
        }

        /// <summary>
        /// Invokes a method on the main loop.
        /// </summary>
        /// <param name="action">The method.</param>
        /// <param name="priority">The priority with which to invoke the method.</param>
        /// <returns>A task that can be used to track the method's execution.</returns>
        public Task InvokeAsync(Action action, DispatcherPriority priority)
        {
            var job = new Job(action, priority);

            lock (this.queue)
            {
                this.queue.Add(job, priority);
            }

            platform.Wake();
            return job.TaskCompletionSource.Task;
        }

        /// <summary>
        /// A job to run.
        /// </summary>
        private class Job
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Job"/> class.
            /// </summary>
            /// <param name="action">The method to call.</param>
            /// <param name="priority">The job priority.</param>
            public Job(Action action, DispatcherPriority priority)
            {
                this.Action = action;
                this.Priority = priority;
                this.TaskCompletionSource = new TaskCompletionSource<object>();
            }

            /// <summary>
            /// Gets the method to call.
            /// </summary>
            public Action Action { get; }

            /// <summary>
            /// Gets the job priority.
            /// </summary>
            public DispatcherPriority Priority { get; }

            /// <summary>
            /// Gets the task completion source.
            /// </summary>
            public TaskCompletionSource<object> TaskCompletionSource { get; }
        }
    }
}
