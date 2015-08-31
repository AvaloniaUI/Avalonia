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
    using Perspex.Controls.Templates;
    using Perspex.Styling;

    /// <summary>
    /// The default style for the <see cref="ScrollViewer"/> control.
    /// </summary>
    public class ScrollViewerStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollViewerStyle"/> class.
        /// </summary>
        public ScrollViewerStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ScrollViewer>())
                {
                    Setters = new[]
                    {
                        new Setter(ScrollViewer.TemplateProperty, new ControlTemplate<ScrollViewer>(Template)),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="ScrollViewer"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(ScrollViewer control)
        {
            var result = new Grid
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
                        Name = "contentPresenter",
                        [~ScrollContentPresenter.ContentProperty] = control[~ScrollViewer.ContentProperty],
                        [~~ScrollContentPresenter.ExtentProperty] = control[~~ScrollViewer.ExtentProperty],
                        [~~ScrollContentPresenter.OffsetProperty] = control[~~ScrollViewer.OffsetProperty],
                        [~~ScrollContentPresenter.ViewportProperty] = control[~~ScrollViewer.ViewportProperty],
                        [~ScrollContentPresenter.CanScrollHorizontallyProperty] = control[~ScrollViewer.CanScrollHorizontallyProperty],
                    },
                    new ScrollBar
                    {
                        Name = "horizontalScrollBar",
                        Orientation = Orientation.Horizontal,
                        [~ScrollBar.MaximumProperty] = control[~ScrollViewer.HorizontalScrollBarMaximumProperty],
                        [~~ScrollBar.ValueProperty] = control[~~ScrollViewer.HorizontalScrollBarValueProperty],
                        [~ScrollBar.ViewportSizeProperty] = control[~ScrollViewer.HorizontalScrollBarViewportSizeProperty],
                        [~ScrollBar.VisibilityProperty] = control[~ScrollViewer.HorizontalScrollBarVisibilityProperty],
                        [Grid.RowProperty] = 1,
                    },
                    new ScrollBar
                    {
                        Name = "verticalScrollBar",
                        Orientation = Orientation.Vertical,
                        [~ScrollBar.MaximumProperty] = control[~ScrollViewer.VerticalScrollBarMaximumProperty],
                        [~~ScrollBar.ValueProperty] = control[~~ScrollViewer.VerticalScrollBarValueProperty],
                        [~ScrollBar.ViewportSizeProperty] = control[~ScrollViewer.VerticalScrollBarViewportSizeProperty],
                        [~ScrollBar.VisibilityProperty] = control[~ScrollViewer.VerticalScrollBarVisibilityProperty],
                        [Grid.ColumnProperty] = 1,
                    },
                },
            };

            return result;
        }
    }
}
