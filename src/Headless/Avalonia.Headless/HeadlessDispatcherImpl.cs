using System;
using Avalonia.Controls.Platform;

namespace Avalonia.Headless;

internal class HeadlessDispatcherImpl : ManagedDispatcherImpl
{
    private readonly TimeProvider _timeProvider;
    private readonly long _startingTimestamp;

    public HeadlessDispatcherImpl(TimeProvider timeProvider) : base(null)
    {
        _timeProvider = timeProvider;
        _startingTimestamp = _timeProvider.GetTimestamp();
    }

    private protected override TimeSpan GetElapsedTime() => _timeProvider.GetElapsedTime(_startingTimestamp);
}
