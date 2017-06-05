// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Layout.UnitTests
{
    internal class TestLayoutRoot : Decorator, ILayoutRoot, IRenderRoot
    {
        public TestLayoutRoot()
        {
            ClientSize = new Size(500, 500);
        }

        public Size ClientSize
        {
            get;
            set;
        }

        public IRenderer Renderer => null;

        public IRenderTarget CreateRenderTarget() => null;

        public void Invalidate(Rect rect)
        {
        }

        public Point PointToClient(Point point) => point;

        public Point PointToScreen(Point point) => point;

        public Size MaxClientSize => Size.Infinity;
        public double LayoutScaling => 1;

        public ILayoutManager LayoutManager { get; set; } = new LayoutManager();
        
    }
}
