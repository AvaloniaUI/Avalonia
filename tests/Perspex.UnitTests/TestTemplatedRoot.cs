// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Perspex.Layout;
using Perspex.Platform;
using Perspex.Rendering;
using Perspex.Styling;

namespace Perspex.UnitTests
{
    public class TestTemplatedRoot : ContentControl, ILayoutRoot, INameScope, IRenderRoot, IStyleRoot
    {
        private readonly NameScope _nameScope = new NameScope();

        public TestTemplatedRoot()
        {
            Template = new FuncControlTemplate<TestTemplatedRoot>(x => new ContentPresenter());
        }

        public event EventHandler<NameScopeEventArgs> Registered
        {
            add { _nameScope.Registered += value; }
            remove { _nameScope.Registered -= value; }
        }

        public event EventHandler<NameScopeEventArgs> Unregistered
        {
            add { _nameScope.Unregistered += value; }
            remove { _nameScope.Unregistered -= value; }
        }

        public Size ClientSize => new Size(100, 100);

        public Size MaxClientSize => Size.Infinity;

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
