// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls;
using Perspex.Platform;
using Perspex.Rendering;

namespace Perspex.Markup.UnitTests
{
    public class TestRoot : Decorator, IRenderRoot, INameScope
    {
        private NameScope _nameScope = new NameScope();

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

        public IRenderTarget RenderTarget
        {
            get { throw new NotImplementedException(); }
        }

        public IRenderQueueManager RenderQueueManager
        {
            get { throw new NotImplementedException(); }
        }

        public Point TranslatePointToScreen(Point p)
        {
            throw new NotImplementedException();
        }

        public void Register(string name, object element)
        {
            _nameScope.Register(name, element);
        }

        public object Find(string name)
        {
            return _nameScope.Find(name);
        }

        public void Unregister(string name)
        {
            _nameScope.Unregister(name);
        }
    }
}
