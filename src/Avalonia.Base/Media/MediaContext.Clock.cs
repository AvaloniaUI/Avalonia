using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Animation;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Media;

internal partial class MediaContext
{
    private readonly MediaContextClock _clock;
    public IGlobalClock Clock => _clock;
    private readonly Stopwatch _time = Stopwatch.StartNew();
    
    class MediaContextClock : IGlobalClock
    {
        private readonly MediaContext _parent;
        private List<IObserver<TimeSpan>> _observers = new();
        public bool HasNewSubscriptions { get; set; }
        public bool HasSubscriptions => _observers.Count > 0;

        public MediaContextClock(MediaContext parent)
        {
            _parent = parent;
        }

        public IDisposable Subscribe(IObserver<TimeSpan> observer)
        {
            _parent.ScheduleRender(false);
            Dispatcher.UIThread.VerifyAccess();
            HasNewSubscriptions = true;
            _observers.Add(observer);
            return Disposable.Create(() =>
            {
                Dispatcher.UIThread.VerifyAccess();
                _observers.Remove(observer);
            });
        }

        public void Pulse(TimeSpan now)
        {
            foreach (var observer in _observers.ToArray())
                observer.OnNext(now);
        }

        public PlayState PlayState
        {
            get => PlayState.Run;
            set => throw new InvalidOperationException();
        }
    }
}