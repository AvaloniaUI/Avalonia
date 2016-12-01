// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Threading
{
    /// <summary>
    /// Provides services for managing work items on a thread.
    /// </summary>
    /// <remarks>
    /// In Avalonia, there is usually only a single <see cref="Dispatcher"/> in the application -
    /// the one for the UI thread, retrieved via the <see cref="UIThread"/> property.
    /// </remarks>
    public class Dispatcher : IDispatcher
    {
        private readonly IPlatformThreadingInterface _platform;
        private readonly JobRunner _jobRunner;

        public static Dispatcher UIThread { get; } =
            new Dispatcher(AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>());

        public Dispatcher(IPlatformThreadingInterface platform)
        {
            _platform = platform;
            if(_platform == null)
                //TODO: Unit test mode, fix that somehow
                return;
            _jobRunner = new JobRunner(platform);
            _platform.Signaled += _jobRunner.RunJobs;
        }

        /// <inheritdoc/>
        public bool CheckAccess() => _platform?.CurrentThreadIsLoopThread ?? true;

        /// <inheritdoc/>
        public void VerifyAccess()
        {
            if (!CheckAccess())
                throw new InvalidOperationException("Call from invalid thread");
        }

        /// <summary>
        /// Runs the dispatcher's main loop.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token used to exit the main loop.
        /// </param>
        public void MainLoop(CancellationToken cancellationToken)
        {
            var platform = AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>();
            cancellationToken.Register(platform.Signal);
            platform.RunLoop(cancellationToken);
        }

        /// <summary>
        /// Runs continuations pushed on the loop.
        /// </summary>
        public void RunJobs()
        {
            _jobRunner?.RunJobs();
        }

        /// <inheritdoc/>
        public Task InvokeTaskAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return _jobRunner?.InvokeAsync(action, priority);
        }

        /// <inheritdoc/>
        public void InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            _jobRunner?.Post(action, priority);
        }
    }
}