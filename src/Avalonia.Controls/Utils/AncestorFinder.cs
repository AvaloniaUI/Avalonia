using System; 
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls.Utils
{
    public static class AncestorFinder
    {
        class FinderNode : IDisposable
        {
            private readonly IControl _control;
            private readonly TypeInfo _ancestorType;
            public IObservable<IControl> Observable => _subject;
            private readonly Subject<IControl> _subject = new Subject<IControl>();

            private FinderNode _child;
            private IDisposable _disposable;

            public FinderNode(IControl control, TypeInfo ancestorType)
            {
                _control = control;
                _ancestorType = ancestorType;
            }

            public void Init()
            {
                _disposable = _control.GetObservable(Control.ParentProperty).Subscribe(OnValueChanged);
            }

            private void OnValueChanged(IControl next)
            {
                if (next == null || _ancestorType.IsAssignableFrom(next.GetType().GetTypeInfo()))
                    _subject.OnNext(next);
                else
                {
                    _child?.Dispose();
                    _child = new FinderNode(next, _ancestorType);
                    _child.Observable.Subscribe(OnChildValueChanged);
                    _child.Init();
                }
            }

            private void OnChildValueChanged(IControl control) => _subject.OnNext(control);


            public void Dispose()
            {
                _disposable.Dispose();
            }
        }


        public static IObservable<IControl> Create(IControl control, Type ancestorType)
        {
            return new AnonymousObservable<IControl>(observer =>
            {
                var finder = new FinderNode(control, ancestorType.GetTypeInfo());
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
