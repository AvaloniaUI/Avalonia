// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Perspex.Win32.Threading;

namespace Perspex.Threading
{
    /// <summary>
    /// Provides services for managing work items on a thread.
    /// </summary>
    /// <remarks>
    /// In Perspex, there is usually only a single <see cref="Dispatcher"/> in the application -
    /// the one for the UI thread, retrieved via the <see cref="UIThread"/> property.
    /// </remarks>
    public class Dispatcher
    {
        private static Dispatcher s_instance = new Dispatcher();

        private MainLoop _mainLoop = new MainLoop();

        /// <summary>
        /// Initializes a new instance of the <see cref="Dispatcher"/> class.
        /// </summary>
        private Dispatcher()
        {
        }

        /// <summary>
        /// Gets the <see cref="Dispatcher"/> for the UI thread.
        /// </summary>
        public static Dispatcher UIThread
        {
            get { return s_instance; }
        }

        /// <summary>
        /// Runs the dispatcher's main loop.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token used to exit the main loop.
        /// </param>
        public void MainLoop(CancellationToken cancellationToken)
        {
            _mainLoop.Run(cancellationToken);
        }

        /// <summary>
        /// Runs continuations pushed on the loop.
        /// </summary>
        public void RunJobs()
        {
            _mainLoop.RunJobs();
        }

        /// <summary>
        /// Invokes a method on the dispatcher thread.
        /// </summary>
        /// <param name="action">The method.</param>
        /// <param name="priority">The priority with which to invoke the method.</param>
        /// <returns>A task that can be used to track the method's execution.</returns>
        public Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return _mainLoop.InvokeAsync(action, priority);
        }

        /// <summary>
        /// Post action that will be invoked on main thread
        /// </summary>
        /// <param name="action">The method.</param>
        /// <param name="priority">The priority with which to invoke the method.</param>
        internal void Post(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            _mainLoop.Post(action, priority);
        }
    }
}