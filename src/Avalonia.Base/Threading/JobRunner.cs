using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Threading
{
    /// <summary>
    /// A main loop in a <see cref="Dispatcher"/>.
    /// </summary>
    internal class JobRunner
    {
        private IPlatformThreadingInterface? _platform;

        private readonly Queue<IJob>[] _queues = Enumerable.Range(0, (int)DispatcherPriority.MaxValue + 1)
            .Select(_ => new Queue<IJob>()).ToArray();

        public JobRunner(IPlatformThreadingInterface? platform)
        {
            _platform = platform;
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

                job.Run();
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
            return job.Task!;
        }

        /// <summary>
        /// Invokes a method on the main loop.
        /// </summary>
        /// <param name="function">The method.</param>
        /// <param name="priority">The priority with which to invoke the method.</param>
        /// <returns>A task that can be used to track the method's execution.</returns>
        public Task<TResult> InvokeAsync<TResult>(Func<TResult> function, DispatcherPriority priority)
        {
            var job = new JobWithResult<TResult>(function, priority);
            AddJob(job);
            return job.Task;
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
        /// Post action that will be invoked on main thread
        /// </summary>
        /// <param name="action">The method to call.</param>
        /// <param name="parameter">The parameter of method to call.</param>
        /// <param name="priority">The priority with which to invoke the method.</param>
        internal void Post(SendOrPostCallback action, object? parameter, DispatcherPriority priority)
        {
            AddJob(new JobWithArg(action, parameter, priority, true));
        }

        /// <summary>
        /// Allows unit tests to change the platform threading interface.
        /// </summary>
        internal void UpdateServices()
        {
            _platform = AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>();
        }

        private void AddJob(IJob job)
        {
            bool needWake;
            var queue = _queues[(int)job.Priority];
            lock (queue)
            {
                needWake = queue.Count == 0;
                queue.Enqueue(job);
            }
            if (needWake)
                _platform?.Signal(job.Priority);
        }

        private IJob? GetNextJob(DispatcherPriority minimumPriority)
        {
            for (int c = (int)DispatcherPriority.MaxValue; c >= (int)minimumPriority; c--)
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

        public bool HasJobsWithPriority(DispatcherPriority minimumPriority)
        {
            for (int c = (int)minimumPriority; c < (int)DispatcherPriority.MaxValue; c++)
            {
                var q = _queues[c];
                lock (q)
                {
                    if (q.Count > 0)
                        return true;
                }
            }

            return false;
        }

        private interface IJob
        {
            /// <summary>
            /// Gets the job priority.
            /// </summary>
            DispatcherPriority Priority { get; }

            /// <summary>
            /// Runs the job.
            /// </summary>
            void Run();
        }

        /// <summary>
        /// A job to run.
        /// </summary>
        private sealed class Job : IJob
        {
            /// <summary>
            /// The method to call.
            /// </summary>
            private readonly Action _action;
            /// <summary>
            /// The task completion source.
            /// </summary>
            private readonly TaskCompletionSource<object?>? _taskCompletionSource;

            /// <summary>
            /// Initializes a new instance of the <see cref="Job"/> class.
            /// </summary>
            /// <param name="action">The method to call.</param>
            /// <param name="priority">The job priority.</param>
            /// <param name="throwOnUiThread">Do not wrap exception in TaskCompletionSource</param>
            public Job(Action action, DispatcherPriority priority, bool throwOnUiThread)
            {
                _action = action;
                Priority = priority;
                _taskCompletionSource = throwOnUiThread ? null : new TaskCompletionSource<object?>();
            }

            /// <inheritdoc/>
            public DispatcherPriority Priority { get; }

            /// <summary>
            /// The task.
            /// </summary>
            public Task? Task => _taskCompletionSource?.Task;
            
            /// <inheritdoc/>
            void IJob.Run()
            {
                if (_taskCompletionSource == null)
                {
                    _action();
                    return;
                }
                try
                {
                    _action();
                    _taskCompletionSource.SetResult(null);
                }
                catch (Exception e)
                {
                    _taskCompletionSource.SetException(e);
                }
            }
        }

        /// <summary>
        /// A typed job to run.
        /// </summary>
        private sealed class JobWithArg : IJob
        {
            private readonly SendOrPostCallback _action;
            private readonly object? _parameter;
            private readonly TaskCompletionSource<bool>? _taskCompletionSource;

            /// <summary>
            /// Initializes a new instance of the <see cref="Job"/> class.
            /// </summary>
            /// <param name="action">The method to call.</param>
            /// <param name="parameter">The parameter of method to call.</param>
            /// <param name="priority">The job priority.</param>
            /// <param name="throwOnUiThread">Do not wrap exception in TaskCompletionSource</param>

            public JobWithArg(SendOrPostCallback action, object? parameter, DispatcherPriority priority, bool throwOnUiThread)
            {
                _action = action;
                _parameter = parameter;
                Priority = priority;
                _taskCompletionSource = throwOnUiThread ? null : new TaskCompletionSource<bool>();
            }

            /// <inheritdoc/>
            public DispatcherPriority Priority { get; }

            /// <inheritdoc/>
            void IJob.Run()
            {
                if (_taskCompletionSource == null)
                {
                    _action(_parameter);
                    return;
                }
                try
                {
                    _action(_parameter);
                    _taskCompletionSource.SetResult(default);
                }
                catch (Exception e)
                {
                    _taskCompletionSource.SetException(e);
                }
            }
        }

        /// <summary>
        /// A job to run thath return value.
        /// </summary>
        /// <typeparam name="TResult">Type of job result</typeparam>
        private sealed class JobWithResult<TResult> : IJob
        {
            private readonly Func<TResult> _function;
            private readonly TaskCompletionSource<TResult> _taskCompletionSource;

            /// <summary>
            /// Initializes a new instance of the <see cref="Job"/> class.
            /// </summary>
            /// <param name="function">The method to call.</param>
            /// <param name="priority">The job priority.</param>
            public JobWithResult(Func<TResult> function, DispatcherPriority priority)
            {
                _function = function;
                Priority = priority;
                _taskCompletionSource = new TaskCompletionSource<TResult>();
            }

            /// <inheritdoc/>
            public DispatcherPriority Priority { get; }

            /// <summary>
            /// The task.
            /// </summary>
            public Task<TResult> Task => _taskCompletionSource.Task;

            /// <inheritdoc/>
            void IJob.Run()
            {
                try
                {
                    var result = _function();
                    _taskCompletionSource.SetResult(result);
                }
                catch (Exception e)
                {
                    _taskCompletionSource.SetException(e);
                }
            }
        }
    }
}
