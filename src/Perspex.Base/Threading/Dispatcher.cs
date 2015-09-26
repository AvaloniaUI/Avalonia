// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Perspex.Platform;

namespace Perspex.Threading
{
    /// <summary>
    /// Provides services for managing work items on a thread.
    /// </summary>
    /// <remarks>
    /// In Perspex, there is usually only a single <see cref="Dispatcher"/> in the application -
    /// the one for the UI thread, retrieved via the <see cref="UIThread"/> property.
    /// </remarks>
    public static class Dispatcher
    {
        private static readonly JobRunner _jobRunner =
            new JobRunner(PerspexLocator.Current.GetService<IPlatformThreadingInterface>());

        /// <summary>
        /// Initializes a new instance of the <see cref="Dispatcher"/> class.
        /// </summary>
        static Dispatcher()
        {
            PerspexLocator.Current.GetService<IPlatformThreadingInterface>().Signaled += _jobRunner.RunJobs;
        }

        /// <summary>
        /// Runs the dispatcher's main loop.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token used to exit the main loop.
        /// </param>
        public static void MainLoop(CancellationToken cancellationToken)
        {
            var platform = PerspexLocator.Current.GetService<IPlatformThreadingInterface>();
            cancellationToken.Register(platform.Signal);
            platform.RunLoop(cancellationToken);
        }

        /// <summary>
        /// Runs continuations pushed on the loop.
        /// </summary>
        public static void RunJobs()
        {
            _jobRunner.RunJobs();
        }

        /// <summary>
        /// Invokes a method on the dispatcher thread.
        /// </summary>
        /// <param name="action">The method.</param>
        /// <param name="priority">The priority with which to invoke the method.</param>
        /// <returns>A task that can be used to track the method's execution.</returns>
        public static Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return _jobRunner.InvokeAsync(action, priority);
        }

        /// <summary>
        /// Post action that will be invoked on main thread
        /// </summary>
        /// <param name="action">The method.</param>
        /// <param name="priority">The priority with which to invoke the method.</param>
        internal static void Post(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            _jobRunner.Post(action, priority);
        }
    }
}