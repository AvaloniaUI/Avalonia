using System;
using System.Diagnostics;

namespace Avalonia.Compatibility;

/// Minimal netstandard2.0 compatible abstraction over TimeProvider.
internal interface IAvnTimeProvider
{
    long GetTimestamp();
    TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp);
}

internal class StopwatchTimeProvider : IAvnTimeProvider
{
    public long GetTimestamp() => Stopwatch.GetTimestamp();

    public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) => new(
        (long)((endingTimestamp - startingTimestamp) * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)));
}
