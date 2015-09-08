





namespace Perspex.Themes.Default
{
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Styling;

    /// <summary>
    /// The default style for the <see cref="TabControl"/> control.
    /// </summary>
    public class TabControlStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TabControlStyle"/> class.
        /// </summary>
        public TabControlStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<TabControl>())
                {
                    Setters = new[]
                    {
                        new Setter(TabControl.TemplateProperty, new ControlTemplate<TabControl>(Template)),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="TabControl"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(TabControl control)
        {
            return new Grid
            {
                RowDefinitions = new RowDefinitions
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(new GridLength(1, GridUnitType.Star)),
                },
                Children = new Controls
                {
                    new TabStrip
                    {
                        Name = "tabStrip",
                        [!TabStrip.ItemsProperty] = control[!TabControl.ItemsProperty],
                        [!!TabStrip.SelectedItemProperty] = control[!!TabControl.SelectedItemProperty],
                    },
                    new Deck
                    {
                        Name = "deck",
                        DataTemplates = new DataTemplates
                        {
                            new DataTemplate<TabItem>(x => (Control)control.MaterializeDataTemplate(x.Content)),
                        },
                        [!Deck.ItemsProperty] = control[!TabControl.ItemsProperty],
                        [!Deck.SelectedItemProperty] = control[!TabControl.SelectedItemProperty],
                        [~Deck.TransitionProperty] = control[~TabControl.TransitionProperty],
                        [Grid.RowProperty] = 1,
                    }
                }
            };
        }
    }
}
