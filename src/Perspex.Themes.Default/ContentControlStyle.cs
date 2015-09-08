





namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Templates;
    using Perspex.Styling;

    /// <summary>
    /// The default style for the <see cref="ContentControl"/> control.
    /// </summary>
    public class ContentControlStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentControlStyle"/> class.
        /// </summary>
        public ContentControlStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ContentControl>())
                {
                    Setters = new[]
                    {
                        new Setter(ContentControl.TemplateProperty, new ControlTemplate<ContentControl>(Template)),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for a <see cref="ContentControl"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(ContentControl control)
        {
            return new Border
            {
                [~Border.BackgroundProperty] = control[~ContentControl.BackgroundProperty],
                Child = new ContentPresenter
                {
                    Name = "contentPresenter",
                    [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
                }
            };
        }
    }
}
