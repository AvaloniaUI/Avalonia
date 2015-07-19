// -----------------------------------------------------------------------
// <copyright file="GridSplitterStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using Perspex.Controls;
    using Perspex.Styling;
    using System.Linq;

    public class GridSplitterStyle : Styles
    {
        public GridSplitterStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<GridSplitter>())
                {
                    Setters = new[]
                    {
                        new Setter(GridSplitter.TemplateProperty, ControlTemplate.Create<GridSplitter>(this.Template)),
                        new Setter(GridSplitter.WidthProperty, 4.0),
                    },
                },
            });
        }

        private Control Template(GridSplitter control)
        {
            Border border = new Border
            {
                [~Border.BackgroundProperty] = control[~GridSplitter.BackgroundProperty],
            };

            return border;
        }
    }
}
