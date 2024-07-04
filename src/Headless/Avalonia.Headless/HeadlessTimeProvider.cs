using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Platform;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Headless;

internal class HeadlessTimeProvider : TimeProvider
{
    public static HeadlessTimeProvider GetCurrent() =>
        AvaloniaLocator.Current.GetRequiredService<HeadlessTimeProvider>();

    private readonly object _sync = new();

    private bool _isRunning;
    private TimeSpan _snapshotTime;

    private TimeProvider _nested;
    private long _nestedStartTimestamp;

    public HeadlessTimeProvider(bool autoStart)
    {
        SetNested(System);
    }

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    
    public override long GetTimestamp()
    {
        lock (_sync)
        {
            return (_snapshotTime + GetTimeAfterStarted()).Ticks;
        }
    }

    public void Pause()
    {
        lock (_sync)
        {
            _snapshotTime += GetTimeAfterStarted();
            _isRunning = false;
        }
    }

    public void Play()
    {
        lock (_sync)
        {
            _isRunning = true;
            _nestedStartTimestamp = _nested.GetTimestamp();
        }
    }

    public void Pulse(TimeSpan time)
    {
        // We can technically allow negative time spans. But should we do that?
        if (time < TimeSpan.Zero)
        {
            throw new ArgumentException("Only non-negative TimeSpan argument is allowed.", nameof(time));
        }

        if (time == default)
        {
            return;
        }

        lock (_sync)
        {
            _snapshotTime += time;
        }
    }

    [MemberNotNull(nameof(_nested))]
    public void SetNested(TimeProvider timeProvider)
    {
        lock (_sync)
        {
            if (timeProvider == _nested)
            {
                return;
            }

            Pause();
            _nested = timeProvider;
            Play();
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            Pause();
            _snapshotTime = default;
            _nested = System;
            Play();
        }
    }

    private TimeSpan GetTimeAfterStarted()
    {
        lock (_sync)
        {
            return _isRunning ? _nested.GetElapsedTime(_nestedStartTimestamp) : default;
        }
    }
}
