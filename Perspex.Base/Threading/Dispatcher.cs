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

    public class Dispatcher
    {
        private static Dispatcher instance = new Dispatcher();

        private MainLoop mainLoop = new MainLoop();

        private Dispatcher()
        {
        }

        public static Dispatcher UIThread
        {
            get { return instance; }
        }

        public void MainLoop(CancellationToken cancellationToken)
        {
            this.mainLoop.Run(cancellationToken);
        }

        public Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return this.mainLoop.InvokeAsync(action, priority);
        }
    }
}