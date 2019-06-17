// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Moq;

namespace Avalonia.UnitTests
{
    public class TestRoot : Decorator, IFocusScope, ILayoutRoot, IInputRoot, INameScope, IRenderRoot, IStyleRoot
    {
        private readonly NameScope _nameScope = new NameScope();

        public TestRoot()
        {
            FocusManager = new FocusManager(this);
            Renderer = Mock.Of<IRenderer>();
        }

        public TestRoot(IControl child)
            : this()
        {
            Child = child;
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

        public Size ClientSize { get; set; } = new Size(100, 100);

        public Size MaxClientSize { get; set; } = Size.Infinity;

        public double LayoutScaling => 1;

        public ILayoutManager LayoutManager { get; set; } = new LayoutManager();

        public double RenderScaling => 1;

        public IRenderer Renderer { get; set; }

        public IAccessKeyHandler AccessKeyHandler => null;

        public IKeyboardNavigationHandler KeyboardNavigationHandler => null;

        public IInputElement PointerOverElement { get; set; }

        public IMouseDevice MouseDevice { get; set; }
        public IFocusManager FocusManager { get; }

        public bool ShowAccessKeys { get; set; }

        public IStyleHost StylingParent { get; set; }

        IStyleHost IStyleHost.StylingParent => StylingParent;

        public IRenderTarget CreateRenderTarget()
        {
            var dc = new Mock<IDrawingContextImpl>();
            dc.Setup(x => x.CreateLayer(It.IsAny<Size>())).Returns(() =>
            {
                var layerDc = new Mock<IDrawingContextImpl>();
                var layer = new Mock<IRenderTargetBitmapImpl>();
                layer.Setup(x => x.CreateDrawingContext(It.IsAny<IVisualBrushRenderer>())).Returns(layerDc.Object);
                return layer.Object;
            });

            var result = new Mock<IRenderTarget>();
            result.Setup(x => x.CreateDrawingContext(It.IsAny<IVisualBrushRenderer>())).Returns(dc.Object);
            return result.Object;
        }

        public void Invalidate(Rect rect)
        {
        }

        public Point PointToClient(PixelPoint p) => p.ToPoint(1);

        public PixelPoint PointToScreen(Point p) => PixelPoint.FromPoint(p, 1);

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
