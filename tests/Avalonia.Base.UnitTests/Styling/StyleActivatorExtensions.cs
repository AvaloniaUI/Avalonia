using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Reactive;
using Avalonia.Styling.Activators;
using Observable = Avalonia.Reactive.Observable;

namespace Avalonia.Base.UnitTests.Styling
{
    internal static class StyleActivatorExtensions
    {
        public static IDisposable Subscribe(this IStyleActivator activator, Action<bool> action)
        {
            return Observable.Subscribe(activator.ToObservable(), action);
        }

        public static async Task<bool> Take(this IStyleActivator activator, int value)
        {
            return await System.Reactive.Linq.Observable.Take(activator.ToObservable(), value);
        }

        public static IObservable<bool> ToObservable(this IStyleActivator activator)
        {
            if (activator == null)
            {
                throw new ArgumentNullException(nameof(activator));
            }

            return new ObservableAdapter(activator);
        }

        private class ObservableAdapter : LightweightObservableBase<bool>, IStyleActivatorSink
        {
            private readonly IStyleActivator _source;
            
            public ObservableAdapter(IStyleActivator source) => _source = source;

            protected override void Initialize() => _source.Subscribe(this);
            protected override void Deinitialize() => _source.Unsubscribe(this);
            
            protected override void Subscribed(IObserver<bool> observer, bool first)
            {
                observer.OnNext(_source.GetIsActive());
            }

            void IStyleActivatorSink.OnNext(bool value)
            {
                PublishNext(value);
            }
        }
    }
}
