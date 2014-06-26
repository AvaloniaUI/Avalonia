// -----------------------------------------------------------------------
// <copyright file="IPlatformThreadingInterface.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;
    using Perspex.Threading;

    public interface IPlatformThreadingInterface
    {
        Dispatcher GetThreadDispatcher();

        void KillTimer(object timerHandle);

        object StartTimer(TimeSpan interval, Action internalTick);
    }
}
