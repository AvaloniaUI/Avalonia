// -----------------------------------------------------------------------
// <copyright file="DefinitionBase.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reactive.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;

namespace Perspex.Controls
{
    public class ScrollViewer : ContentControl
    {
        public static PerspexProperty<Size> ExtentProperty =
            PerspexProperty.Register<ScrollViewer, Size>("Extent");

        public static PerspexProperty<Vector> OffsetProperty =
            PerspexProperty.Register<ScrollViewer, Vector>("Offset");

        public static PerspexProperty<Size> ViewportProperty =
            PerspexProperty.Register<ScrollViewer, Size>("Viewport");

        private ScrollContentPresenter presenter;

        private ScrollBar horizontalScrollBar;

        private ScrollBar verticalScrollBar;

        public Size Extent
        {
            get { return this.GetValue(ExtentProperty); }
            private set { this.SetValue(ExtentProperty, value); }
        }

        public Vector Offset
        {
            get { return this.GetValue(OffsetProperty); }
            set { this.SetValue(OffsetProperty, value); }
        }

        public Size Viewport
        {
            get { return this.GetValue(ViewportProperty); }
            private set { this.SetValue(ViewportProperty, value); }
        }

        protected override void OnTemplateApplied()
        {
            this.presenter = this.GetTemplateChild<ScrollContentPresenter>("presenter");
            this.horizontalScrollBar = this.GetTemplateChild<ScrollBar>("horizontalScrollBar");
            this.verticalScrollBar = this.GetTemplateChild<ScrollBar>("verticalScrollBar");

            this[!ExtentProperty] = this.presenter[!ExtentProperty];
            this[!ViewportProperty] = this.presenter[!ViewportProperty];
            this.presenter[!OffsetProperty] = this[!OffsetProperty];

            var extentAndViewport = Observable.CombineLatest(
                this.GetObservable(ExtentProperty).StartWith(this.Extent),
                this.GetObservable(ViewportProperty).StartWith(this.Viewport))
                .Select(x => new { Extent = x[0], Viewport = x[1] });

            this.horizontalScrollBar.Bind(
                IsVisibleProperty,
                extentAndViewport.Select(x => x.Extent.Width > x.Viewport.Width));

            this.horizontalScrollBar.Bind(
                ScrollBar.MaximumProperty,
                extentAndViewport.Select(x => x.Extent.Width));

            this.horizontalScrollBar.Bind(
                ScrollBar.ViewportSizeProperty,
                extentAndViewport.Select(x => x.Viewport.Width));

            this.verticalScrollBar.Bind(
                IsVisibleProperty,
                extentAndViewport.Select(x => x.Extent.Height > x.Viewport.Height));

            this.verticalScrollBar.Bind(
                ScrollBar.MaximumProperty,
                extentAndViewport.Select(x => x.Extent.Height));

            this.verticalScrollBar.Bind(
                ScrollBar.ViewportSizeProperty,
                extentAndViewport.Select(x => x.Viewport.Height));

            var offset = Observable.CombineLatest(
                this.horizontalScrollBar.GetObservable(ScrollBar.ValueProperty),
                this.verticalScrollBar.GetObservable(ScrollBar.ValueProperty))
                .Select(x => new Vector(x[0], x[1]));

            this.Bind(OffsetProperty, offset);
        }
    }
}
