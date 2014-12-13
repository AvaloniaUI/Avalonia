// -----------------------------------------------------------------------
// <copyright file="ScrollViewerStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Media;
    using Perspex.Styling;

    public class ScrollViewerStyle : Styles
    {
        public ScrollViewerStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ScrollViewer>())
                {
                    Setters = new[]
                    {
                        new Setter(ScrollViewer.TemplateProperty, ControlTemplate.Create<ScrollViewer>(this.Template)),
                    },
                },
            });
        }

        private Control Template(ScrollViewer control)
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
                        Id = "presenter",
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
                        [~ScrollBar.IsVisibleProperty] = control[~ScrollViewer.IsHorizontalScrollBarVisibleProperty],
                        [~ScrollBar.MaximumProperty] = control[~ScrollViewer.HorizontalScrollBarMaximumProperty],
                        [~~ScrollBar.ValueProperty] = control[~~ScrollViewer.HorizontalScrollBarValueProperty],
                        [~ScrollBar.ViewportSizeProperty] = control[~ScrollViewer.HorizontalScrollBarViewportSizeProperty],
                        [Grid.RowProperty] = 1,
                    },
                    new ScrollBar
                    {
                        Id = "verticalScrollBar",
                        Orientation = Orientation.Vertical,
                        [~ScrollBar.IsVisibleProperty] = control[~ScrollViewer.IsVerticalScrollBarVisibleProperty],
                        [~ScrollBar.MaximumProperty] = control[~ScrollViewer.VerticalScrollBarMaximumProperty],
                        [~~ScrollBar.ValueProperty] = control[~~ScrollViewer.VerticalScrollBarValueProperty],
                        [~ScrollBar.ViewportSizeProperty] = control[~ScrollViewer.VerticalScrollBarViewportSizeProperty],
                        [Grid.ColumnProperty] = 1,
                    },
                },
            };
        }
    }
}
