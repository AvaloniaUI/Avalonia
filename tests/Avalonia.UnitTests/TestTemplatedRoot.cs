// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;

namespace Avalonia.UnitTests
{
    public class TestTemplatedRoot : ContentControl, ILayoutRoot, IRenderRoot, IStyleRoot
    {
        private readonly NameScope _nameScope = new NameScope();

        public TestTemplatedRoot()
        {
            Template = new FuncControlTemplate<TestTemplatedRoot>((x, scope) => new ContentPresenter
            {
                Name = "PART_ContentPresenter",
            }.RegisterInNameScope(scope));
        }

        public Size ClientSize => new Size(100, 100);

        public Size MaxClientSize => Size.Infinity;

        public double LayoutScaling => 1;

        public ILayoutManager LayoutManager { get; set; } = new LayoutManager();

        public double RenderScaling => 1;

        public IRenderTarget RenderTarget => null;

        public IRenderer Renderer => null;

        public IRenderTarget CreateRenderTarget()
        {
            throw new NotImplementedException();
        }

        public void Invalidate(Rect rect)
        {
            throw new NotImplementedException();
        }

        public Point PointToClient(PixelPoint p) => p.ToPoint(1);

        public PixelPoint PointToScreen(Point p) => PixelPoint.FromPoint(p, 1);
    }
}
