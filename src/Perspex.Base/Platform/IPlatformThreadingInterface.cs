// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Threading;

namespace Perspex.Platform
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
        /// <param name="interval">The interval.</param>
        /// <param name="tick">The action to call on each tick.</param>
        /// <returns>An <see cref="IDisposable"/> used to stop the timer.</returns>
        IDisposable StartTimer(TimeSpan interval, Action tick);

        /// <summary>
        /// Sends a message that causes <see cref="ProcessMessage"/> to exit.
        /// </summary>
        void Signal();

        bool CheckForLoopThread();

        event Action Signaled;

    }
}
