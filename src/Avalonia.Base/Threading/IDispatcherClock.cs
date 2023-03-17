using System;

namespace Avalonia.Threading;

internal interface IDispatcherClock
{
    int TickCount { get; }
}

internal class DefaultDispatcherClock : IDispatcherClock
{
    public int TickCount => Environment.TickCount;
}