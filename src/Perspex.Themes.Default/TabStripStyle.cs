





namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Styling;

    /// <summary>
    /// The default style for the <see cref="TabStrip"/> control.
    /// </summary>
    public class TabStripStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TabStripStyle"/> class.
        /// </summary>
        public TabStripStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<TabStrip>())
                {
                    Setters = new[]
                    {
                        new Setter(TabStrip.TemplateProperty, new ControlTemplate<TabStrip>(Template)),
                    },
                },
                new Style(x => x.OfType<TabStrip>().Template().OfType<StackPanel>())
                {
                    Setters = new[]
                    {
                        new Setter(StackPanel.GapProperty, 16.0),
                        new Setter(StackPanel.OrientationProperty, Orientation.Horizontal),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="TabStrip"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(TabStrip control)
        {
            return new ItemsPresenter
            {
                Name = "itemsPresenter",
                [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
            };
        }
    }
}
