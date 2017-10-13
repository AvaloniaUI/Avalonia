// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Threading
{
    /// <summary>
    /// A main loop in a <see cref="Dispatcher"/>.
    /// </summary>
    internal class JobRunner
    {


        private IPlatformThreadingInterface _platform;

        private Queue<Job>[] _queues = Enumerable.Range(0, (int) DispatcherPriority.MaxValue + 1)
            .Select(_ => new Queue<Job>()).ToArray();

        public JobRunner(IPlatformThreadingInterface platform)
        {
            _platform = platform;
        }

        Job GetNextJob(DispatcherPriority minimumPriority)
        {
            for (int c = (int) DispatcherPriority.MaxValue; c >= (int) minimumPriority; c--)
            {
                var q = _queues[c];
                lock (q)
                {
                    if (q.Count > 0)
                        return q.Dequeue();
                }
            }
            return null;
        }

        /// <summary>
        /// Runs continuations pushed on the loop.
        /// </summary>
        /// <param name="priority">Priority to execute jobs for. Pass null if platform doesn't have internal priority system</param>
        public void RunJobs(DispatcherPriority? priority)
        {
            var minimumPriority = priority ?? DispatcherPriority.MinValue;
            while (true)
            {
                var job = GetNextJob(minimumPriority);
                if (job == null)
                    return;
                

                if (job.TaskCompletionSource == null)
                {
                    job.Action();
                }
                else
                {
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
            var job = new Job(action, priority, false);
            AddJob(job);
            return job.TaskCompletionSource.Task;
        }

        /// <summary>
        /// Post action that will be invoked on main thread
        /// </summary>
        /// <param name="action">The method.</param>
        /// 
        /// <param name="priority">The priority with which to invoke the method.</param>
        internal void Post(Action action, DispatcherPriority priority)
        {
            AddJob(new Job(action, priority, true));
        }

        /// <summary>
        /// Allows unit tests to change the platform threading interface.
        /// </summary>
        internal void UpdateServices()
        {
            _platform = AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>();
        }

        private void AddJob(Job job)
        {
            var needWake = false;
            var queue = _queues[(int) job.Priority];
            lock (queue)
            {
                needWake = queue.Count == 0;
                queue.Enqueue(job);
            }
            if (needWake)
                _platform?.Signal(job.Priority);
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
            /// <param name="throwOnUiThread">Do not wrap excepption in TaskCompletionSource</param>
            public Job(Action action, DispatcherPriority priority, bool throwOnUiThread)
            {
                Action = action;
                Priority = priority;
                TaskCompletionSource = throwOnUiThread ? null : new TaskCompletionSource<object>();
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
