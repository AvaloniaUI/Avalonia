// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Reactive;

namespace Avalonia.LogicalTree
{
    /// <summary>
    /// Locates controls relative to other controls.
    /// </summary>
    public static class ControlLocator
    {
        /// <summary>
        /// Tracks a named control relative to another control.
        /// </summary>
        /// <param name="relativeTo">
        /// The control relative from which the other control should be found.
        /// </param>
        /// <param name="name">The name of the control to find.</param>
        public static IObservable<ILogical> Track(ILogical relativeTo, string name)
        {
            return new ControlTracker(relativeTo, name);
        }

        public static IObservable<ILogical> Track(ILogical relativeTo, int ancestorLevel, Type ancestorType = null)
        {
            return new ControlTracker(relativeTo, ancestorLevel, ancestorType);
        }

        private class ControlTracker : LightweightObservableBase<ILogical>
        {
            private readonly ILogical _relativeTo;
            private readonly string _name;
            private readonly int _ancestorLevel;
            private readonly Type _ancestorType;
            INameScope _nameScope;
            ILogical _value;

            public ControlTracker(ILogical relativeTo, string name)
            {
                _relativeTo = relativeTo;
                _name = name;
            }

            public ControlTracker(ILogical relativeTo, int ancestorLevel, Type ancestorType)
            {
                _relativeTo = relativeTo;
                _ancestorLevel = ancestorLevel;
                _ancestorType = ancestorType;
            }

            protected override void Initialize()
            {
                Update();
                _relativeTo.AttachedToLogicalTree += Attached;
                _relativeTo.DetachedFromLogicalTree += Detached;
            }

            protected override void Deinitialize()
            {
                _relativeTo.AttachedToLogicalTree -= Attached;
                _relativeTo.DetachedFromLogicalTree -= Detached;

                if (_nameScope != null)
                {
                    _nameScope.Registered -= Registered;
                    _nameScope.Unregistered -= Unregistered;
                }

                _value = null;
            }

            protected override void Subscribed(IObserver<ILogical> observer, bool first)
            {
                observer.OnNext(_value);
            }

            private void Attached(object sender, LogicalTreeAttachmentEventArgs e)
            {
                Update();
                PublishNext(_value);
            }

            private void Detached(object sender, LogicalTreeAttachmentEventArgs e)
            {
                if (_nameScope != null)
                {
                    _nameScope.Registered -= Registered;
                    _nameScope.Unregistered -= Unregistered;
                }

                _value = null;
                PublishNext(null);
            }

            private void Registered(object sender, NameScopeEventArgs e)
            {
                if (e.Name == _name && e.Element is ILogical logical)
                {
                    _value = logical;
                    PublishNext(logical);
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

            private void Update()
            {
                if (_name != null)
                {
                    _nameScope = _relativeTo.FindNameScope();

                    if (_nameScope != null)
                    {
                        _nameScope.Registered += Registered;
                        _nameScope.Unregistered += Unregistered;
                        _value = _nameScope.Find<ILogical>(_name);
                    }
                    else
                    {
                        _value = null;
                    }
                }
                else
                {
                    _value = _relativeTo.GetLogicalAncestors()
                        .Where(x => _ancestorType?.GetTypeInfo().IsAssignableFrom(x.GetType().GetTypeInfo()) ?? true)
                        .ElementAtOrDefault(_ancestorLevel);
                }
            }
        }
    }
}
