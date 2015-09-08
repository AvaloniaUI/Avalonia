





namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Templates;
    using Perspex.Styling;

    /// <summary>
    /// The default style for the <see cref="ItemsControl"/> control.
    /// </summary>
    public class ItemsControlStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsControlStyle"/> class.
        /// </summary>
        public ItemsControlStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ItemsControl>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty, new ControlTemplate<ItemsControl>(Template)),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for an <see cref="ItemsControl"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(ItemsControl control)
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
