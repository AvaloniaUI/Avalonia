using System;
using Avalonia.Metadata;

namespace Avalonia.Animation
{
    [NotClientImplementable]
    public interface IClock : IObservable<TimeSpan>
    {
        PlayState PlayState { get; set; }
    }
}
