using System;
using System.Collections;
using Avalonia.Animation;
using MiniMvvm;

namespace RenderDemo.ViewModels;

public class AnimationSpeedPageViewModel : ViewModelBase
{
    private double _speedRatio;
    public double SpeedRatio
    {
        get => _speedRatio;
        set => RaiseAndSetIfChanged(ref _speedRatio, value);
    }

    private static readonly PlaybackDirection[] s_playbackDirections = [
        PlaybackDirection.Normal,
        PlaybackDirection.Reverse,
        PlaybackDirection.Alternate,
        PlaybackDirection.AlternateReverse
    ];
    public PlaybackDirection[] PlaybackDirections
    {
        get => s_playbackDirections;
    }

    private PlaybackDirection GetOppositePlaybackDirection(PlaybackDirection value)
    {
        switch (value)
        {
            case PlaybackDirection.Normal:
                return PlaybackDirection.Reverse;
            case PlaybackDirection.Reverse:
                return PlaybackDirection.Normal;
            case PlaybackDirection.Alternate:
                return PlaybackDirection.AlternateReverse;
            case PlaybackDirection.AlternateReverse:
                return PlaybackDirection.Alternate;
        }
        throw new ArgumentOutOfRangeException();
    }

    private PlaybackDirection _playbackDirection;
    public PlaybackDirection PlaybackDirection
    {
        get => _playbackDirection;
        set
        {
            _playbackDirection = value;
            RaisePropertyChanged(nameof(PlaybackDirection));
            _playbackDirectionOpposite = GetOppositePlaybackDirection(value);
            RaisePropertyChanged(nameof(PlaybackDirectionOpposite));
        }
    }

    private PlaybackDirection _playbackDirectionOpposite;
    public PlaybackDirection PlaybackDirectionOpposite
    {
        get => _playbackDirectionOpposite;
        set
        {
            _playbackDirectionOpposite = value;
            RaisePropertyChanged(nameof(PlaybackDirectionOpposite));
            _playbackDirection = GetOppositePlaybackDirection(value);
            RaisePropertyChanged(nameof(PlaybackDirection));
        }
    }

    private TimeSpan _delay;
    public TimeSpan Delay
    {
        get => _delay;
        set => RaiseAndSetIfChanged(ref _delay, value);
    }

    public double DelayInput
    {
        get => _delay.TotalSeconds;
        set => Delay = TimeSpan.FromSeconds(value);
    }

    private TimeSpan _delayIters;
    public TimeSpan DelayIters
    {
        get => _delayIters;
        set => RaiseAndSetIfChanged(ref _delayIters, value);
    }

    public double DelayItersInput
    {
        get => _delayIters.TotalSeconds;
        set => DelayIters = TimeSpan.FromSeconds(value);
    }

    public AnimationSpeedPageViewModel()
    {
        SpeedRatio = 1;
        PlaybackDirection = PlaybackDirection.Normal;
        DelayInput = 0;
        DelayItersInput = 0;
    }
}
