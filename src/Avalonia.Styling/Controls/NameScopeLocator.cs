using System;
using System.Linq;
using System.Reflection;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    public class NameScopeLocator
    {
        /// <summary>
        /// Tracks a named control relative to another control.
        /// </summary>
        /// <param name="relativeTo">
        /// The control relative from which the other control should be found.
        /// </param>
        /// <param name="name">The name of the control to find.</param>
        public static IObservable<object> Track(INameScope scope, string name)
        {
            return new ScopeTracker(scope, name);
        }
        
        private class ScopeTracker : LightweightObservableBase<object>
        {
            private readonly string _name;
            INameScope _nameScope;
            object _value;

            public ScopeTracker(INameScope nameScope, string name)
            {
                _nameScope = nameScope;
                _name = name;
            }


            protected override void Initialize()
            {
                _nameScope.Registered += Registered;
                _nameScope.Unregistered += Unregistered;
                _value = _nameScope.Find<ILogical>(_name);
            }

            protected override void Deinitialize()
            {
                if (_nameScope != null)
                {
                    _nameScope.Registered -= Registered;
                    _nameScope.Unregistered -= Unregistered;
                }

                _value = null;
            }

            protected override void Subscribed(IObserver<object> observer, bool first)
            {
                observer.OnNext(_value);
            }

            private void Registered(object sender, NameScopeEventArgs e)
            {
                if (e.Name == _name)
                {
                    _value = e.Element;
                    PublishNext(_value);
                }
            }

            private void Unregistered(object sender, NameScopeEventArgs e)
            {
                if (e.Name == _name)
                {
                    _value = null;
                    PublishNext(null);
                }
            }

        }
    }
}
