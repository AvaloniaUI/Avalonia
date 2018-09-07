using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Animation
{
    public interface IClock : IObservable<TimeSpan>
    {
        bool HasSubscriptions { get; }
        TimeSpan CurrentTime { get; }
        PlayState PlayState { get; set; }
    }
}
