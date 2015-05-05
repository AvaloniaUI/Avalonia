// -----------------------------------------------------------------------
// <copyright file="ScrollViewerTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.VisualTree;
    using Xunit;
    using Perspex.Layout;

    public class ScrollViewerTests
    {
        [Fact]
        public void Content_Is_Created()
        {
            var target = new ScrollViewer
            {
                Template = ControlTemplate.Create<ScrollViewer>(this.CreateTemplate),
                Content = "Foo",
            };

            target.ApplyTemplate();

            var presenter = target.GetTemplateChild<ScrollContentPresenter>("contentPresenter");

            Assert.IsType<TextBlock>(presenter.Child);
        }

        [Fact]
        public void ScrollViewer_In_Template_Sets_Child_TemplatedParent()
        {
            var target = new ContentControl
            {
                Template = ControlTemplate.Create<ContentControl>(this.CreateNestedTemplate),
                Content = "Foo",
            };

            target.ApplyTemplate();

            var presenter = target.GetVisualDescendents()
                .OfType<ContentPresenter>()
                .Single(x => x.Id == "this");

            Assert.Equal(target, presenter.TemplatedParent);
        }

        private Control CreateTemplate(ScrollViewer control)
        {
            return new Grid
            {
                ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition(1, GridUnitType.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
                RowDefinitions = new RowDefinitions
                {
                    new RowDefinition(1, GridUnitType.Star),
                    new RowDefinition(GridLength.Auto),
                },
                Children = new Controls
                {
                    new ScrollContentPresenter
                    {
                        Id = "contentPresenter",
                        [~ScrollContentPresenter.ContentProperty] = control[~ScrollViewer.ContentProperty],
                        [~~ScrollContentPresenter.ExtentProperty] = control[~~ScrollViewer.ExtentProperty],
                        [~~ScrollContentPresenter.OffsetProperty] = control[~~ScrollViewer.OffsetProperty],
                        [~~ScrollContentPresenter.ViewportProperty] = control[~~ScrollViewer.ViewportProperty],
                        [~ScrollContentPresenter.CanScrollHorizontallyProperty] = control[~ScrollViewer.CanScrollHorizontallyProperty],
                    },
                    new ScrollBar
                    {
                        Id = "horizontalScrollBar",
                        Orientation = Orientation.Horizontal,
                        [~ScrollBar.MaximumProperty] = control[~ScrollViewer.HorizontalScrollBarMaximumProperty],
                        [~~ScrollBar.ValueProperty] = control[~~ScrollViewer.HorizontalScrollBarValueProperty],
                        [~ScrollBar.ViewportSizeProperty] = control[~ScrollViewer.HorizontalScrollBarViewportSizeProperty],
                        [~ScrollBar.VisibilityProperty] = control[~ScrollViewer.HorizontalScrollBarVisibilityProperty],
                        [Grid.RowProperty] = 1,
                    },
                    new ScrollBar
                    {
                        Id = "verticalScrollBar",
                        Orientation = Orientation.Vertical,
                        [~ScrollBar.MaximumProperty] = control[~ScrollViewer.VerticalScrollBarMaximumProperty],
                        [~~ScrollBar.ValueProperty] = control[~~ScrollViewer.VerticalScrollBarValueProperty],
                        [~ScrollBar.ViewportSizeProperty] = control[~ScrollViewer.VerticalScrollBarViewportSizeProperty],
                        [~ScrollBar.VisibilityProperty] = control[~ScrollViewer.VerticalScrollBarVisibilityProperty],
                        [Grid.ColumnProperty] = 1,
                    },
                },
            };
        }

        private Control CreateNestedTemplate(ContentControl control)
        {
            return new ScrollViewer
            {
                Template = ControlTemplate.Create<ScrollViewer>(this.CreateTemplate),
                Content = new ContentPresenter
                {
                    Id = "this"
                }
            };
        }
    }
}