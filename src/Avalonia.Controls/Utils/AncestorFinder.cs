using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Avalonia.Controls.Utils
{
    public static class AncestorFinder
    {
        class FinderNode : IDisposable
        {
            private readonly StyledElement _control;
            private readonly Type _ancestorType;
            public IObservable<StyledElement?> Observable => _subject;
            private readonly Subject<StyledElement?> _subject = new Subject<StyledElement?>();

            private FinderNode? _child;
            private IDisposable? _disposable;

            public FinderNode(StyledElement control, Type ancestorType)
            {
                _control = control;
                _ancestorType = ancestorType;
            }

            public void Init()
            {
                _disposable = _control.GetObservable(Control.ParentProperty).Subscribe(OnValueChanged);
            }

            private void OnValueChanged(StyledElement? next)
            {
                if (next == null || _ancestorType.IsAssignableFrom(next.GetType()))
                    _subject.OnNext(next);
                else
                {
                    _child?.Dispose();
                    _child = new FinderNode(next, _ancestorType);
                    _child.Observable.Subscribe(OnChildValueChanged);
                    _child.Init();
                }
            }

            private void OnChildValueChanged(StyledElement? control) => _subject.OnNext(control);


            public void Dispose()
            {
                _child?.Dispose();
                _subject.Dispose();
                _disposable?.Dispose();
            }
        }

        public static IObservable<T?> Create<T>(StyledElement control)
            where T : StyledElement
        {
            return Create(control, typeof(T)).Select(x => (T?)x);
        }

        public static IObservable<StyledElement?> Create(StyledElement control, Type ancestorType)
        {
            return new AnonymousObservable<StyledElement?>(observer =>
            {
                var finder = new FinderNode(control, ancestorType);
                var subscription = finder.Observable.Subscribe(observer);
                finder.Init();

                return Disposable.Create(() =>
                {
                    subscription.Dispose();
                    finder.Dispose();
                });
            });
        }
    }
}
