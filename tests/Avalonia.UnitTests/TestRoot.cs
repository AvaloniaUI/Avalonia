// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Moq;

namespace Avalonia.UnitTests
{
    public class TestRoot : Decorator, IFocusScope, ILayoutRoot, INameScope, IRenderRoot, IStyleRoot
    {
        private readonly NameScope _nameScope = new NameScope();
        private readonly IRenderTarget _renderTarget = Mock.Of<IRenderTarget>(
            x => x.CreateDrawingContext(It.IsAny<IVisualBrushRenderer>()) == Mock.Of<IDrawingContextImpl>());

        public TestRoot()
        {
            var rendererFactory = AvaloniaLocator.Current.GetService<IRendererFactory>();
            var renderLoop = AvaloniaLocator.Current.GetService<IRenderLoop>();
            Renderer = rendererFactory?.CreateRenderer(this, renderLoop);
        }

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

        public Size MaxClientSize { get; set; } = Size.Infinity;

        public double LayoutScaling => 1;

        public ILayoutManager LayoutManager => AvaloniaLocator.Current.GetService<ILayoutManager>();

        public IRenderTarget RenderTarget => null;

        public IRenderer Renderer { get; set; }

        public IRenderTarget CreateRenderTarget() => _renderTarget;

        public void Invalidate(Rect rect)
        {
        }

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
