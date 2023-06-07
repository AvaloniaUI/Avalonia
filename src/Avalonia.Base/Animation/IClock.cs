using System;
using Avalonia.Metadata;

namespace Avalonia.Animation
{
    internal interface IClock : IObservable<TimeSpan>
    {
        PlayState PlayState { get; set; }
    }
}
