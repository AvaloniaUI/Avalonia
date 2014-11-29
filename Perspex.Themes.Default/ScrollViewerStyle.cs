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
                        [~ContentPresenter.ContentProperty] = control[~ScrollViewer.ContentProperty],
                    },
                    new ScrollBar
                    {
                        Id = "horizontalScrollBar",
                        Orientation = Orientation.Horizontal,
                        [Grid.RowProperty] = 1,
                    },
                    new ScrollBar
                    {
                        Id = "verticalScrollBar",
                        Orientation = Orientation.Vertical,
                        [Grid.ColumnProperty] = 1,
                    },
                },
            };
        }
    }
}
