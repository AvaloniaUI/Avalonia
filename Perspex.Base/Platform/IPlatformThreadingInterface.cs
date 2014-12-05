// -----------------------------------------------------------------------
// <copyright file="IPlatformThreadingInterface.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Perspex.Threading;

    /// <summary>
    /// Provides platform-specific services relating to threading.
    /// </summary>
    public interface IPlatformThreadingInterface
    {
        /// <summary>
        /// Process a single message from the windowing system, blocking until one is available.
        /// </summary>
        void ProcessMessage();

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
        void Wake();
    }
}
