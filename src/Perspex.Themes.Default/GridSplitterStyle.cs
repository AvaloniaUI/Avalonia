





namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Templates;
    using Perspex.Styling;

    /// <summary>
    /// The default style for the <see cref="GridSplitter"/> control.
    /// </summary>
    public class GridSplitterStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridSplitterStyle"/> class.
        /// </summary>
        public GridSplitterStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<GridSplitter>())
                {
                    Setters = new[]
                    {
                        new Setter(GridSplitter.TemplateProperty, new ControlTemplate<GridSplitter>(Template)),
                        new Setter(GridSplitter.WidthProperty, 4.0),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for a <see cref="GridSplitter"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(GridSplitter control)
        {
            Border border = new Border
            {
                [~Border.BackgroundProperty] = control[~GridSplitter.BackgroundProperty],
            };

            return border;
        }
    }
}
