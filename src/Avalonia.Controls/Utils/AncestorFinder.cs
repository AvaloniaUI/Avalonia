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
            private readonly IStyledElement _control;
            private readonly Type _ancestorType;
            public IObservable<IStyledElement?> Observable => _subject;
            private readonly Subject<IStyledElement?> _subject = new Subject<IStyledElement?>();

            private FinderNode? _child;
            private IDisposable? _disposable;

            public FinderNode(IStyledElement control, Type ancestorType)
            {
                _control = control;
                _ancestorType = ancestorType;
            }

            public void Init()
            {
                _disposable = _control.GetObservable(Control.ParentProperty).Subscribe(OnValueChanged);
            }

            private void OnValueChanged(IStyledElement? next)
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

            private void OnChildValueChanged(IStyledElement? control) => _subject.OnNext(control);


            public void Dispose()
            {
                _child?.Dispose();
                _subject.Dispose();
                _disposable?.Dispose();
            }
        }

        public static IObservable<T?> Create<T>(IStyledElement control)
            where T : IStyledElement
        {
            return Create(control, typeof(T)).Select(x => (T?)x);
        }

        public static IObservable<IStyledElement?> Create(IStyledElement control, Type ancestorType)
        {
            return new AnonymousObservable<IStyledElement?>(observer =>
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
