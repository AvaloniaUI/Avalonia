// -----------------------------------------------------------------------
// <copyright file="Dispatcher.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Threading
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Perspex.Win32.Threading;

    /// <summary>
    /// Provides services for managing work items on a thread.
    /// </summary>
    /// <remarks>
    /// In Perspex, there is usually only a single <see cref="Dispatcher"/> in the application -
    /// the one for the UI thread, retrieved via the <see cref="UIThread"/> property.
    /// </remarks>
    public class Dispatcher
    {
        private static Dispatcher instance = new Dispatcher();

        private MainLoop mainLoop = new MainLoop();

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
            get { return instance; }
        }

        /// <summary>
        /// Runs the dispatcher's main loop.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token used to exit the main loop.
        /// </param>
        public void MainLoop(CancellationToken cancellationToken)
        {
            this.mainLoop.Run(cancellationToken);
        }

        /// <summary>
        /// Invokes a method on the dispatcher thread.
        /// </summary>
        /// <param name="action">The method.</param>
        /// <param name="priority">The priority with which to invoke the method.</param>
        /// <returns>A task that can be used to track the method's execution.</returns>
        public Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return this.mainLoop.InvokeAsync(action, priority);
        }
    }
}