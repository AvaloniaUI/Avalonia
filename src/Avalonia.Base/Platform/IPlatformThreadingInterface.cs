// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading;
using Avalonia.Threading;

namespace Avalonia.Platform
{
    /// <summary>
    /// Provides platform-specific services relating to threading.
    /// </summary>
    public interface IPlatformThreadingInterface
    {
        void RunLoop(CancellationToken cancellationToken);

        /// <summary>
        /// Starts a timer.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="interval">The interval.</param>
        /// <param name="tick">The action to call on each tick.</param>
        /// <returns>An <see cref="IDisposable"/> used to stop the timer.</returns>
        IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick);

        void Signal(DispatcherPriority priority);

        bool CurrentThreadIsLoopThread { get; }

        event Action<DispatcherPriority?> Signaled;
    }
}
