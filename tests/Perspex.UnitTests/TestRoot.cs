// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls;
using Perspex.Input;
using Perspex.Layout;
using Perspex.Platform;
using Perspex.Rendering;
using Perspex.Styling;

namespace Perspex.UnitTests
{
    public class TestRoot : Decorator, IFocusScope, ILayoutRoot, INameScope, IRenderRoot, IStyleRoot
    {
        private readonly NameScope _nameScope = new NameScope();

        event EventHandler<NameScopeEventArgs> INameScope.Registered
        {
            add { _nameScope.Registered += value; ++NameScopeRegisteredSubscribers; }
            remove { _nameScope.Registered -= value; --NameScopeRegisteredSubscribers; }
        }

        public event EventHandler<NameScopeEventArgs> Unregistered
        {
            add { _nameScope.Unregistered += value; ++NameScopeUnregisteredSubscribers; }
            remove { _nameScope.Unregistered -= value; --NameScopeUnregisteredSubscribers; }
        }

        public int NameScopeRegisteredSubscribers { get; private set; }

        public int NameScopeUnregisteredSubscribers { get; private set; }

        public Size ClientSize => new Size(100, 100);

        public Size MaxClientSize => Size.Infinity;

        public double LayoutScaling => 1;

        public ILayoutManager LayoutManager => PerspexLocator.Current.GetService<ILayoutManager>();

        public IRenderTarget RenderTarget => null;

        public IRenderQueueManager RenderQueueManager => null;

        public Point PointToClient(Point p) => p;

        public Point PointToScreen(Point p) => p;

        void INameScope.Register(string name, object element)
        {
            _nameScope.Register(name, element);
        }

        object INameScope.Find(string name)
        {
            return _nameScope.Find(name);
        }

        void INameScope.Unregister(string name)
        {
            _nameScope.Unregister(name);
        }
    }
}
