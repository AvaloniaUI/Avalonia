// -----------------------------------------------------------------------
// <copyright file="DeckStyle.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Templates;
    using Perspex.Styling;

    /// <summary>
    /// The default style for the <see cref="Deck"/> control.
    /// </summary>
    public class DeckStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeckStyle"/> class.
        /// </summary>
        public DeckStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<Deck>())
                {
                    Setters = new[]
                    {
                        new Setter(Deck.TemplateProperty, new ControlTemplate<Deck>(Template)),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="Deck"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(Deck control)
        {
            return new DeckPresenter
            {
                Name = "itemsPresenter",
                [~ItemsPresenter.ItemsProperty] = control[~Deck.ItemsProperty],
                [~ItemsPresenter.ItemsPanelProperty] = control[~Deck.ItemsPanelProperty],
                [~DeckPresenter.SelectedIndexProperty] = control[~Deck.SelectedIndexProperty],
                [~DeckPresenter.TransitionProperty] = control[~Deck.TransitionProperty],
            };
        }
    }
}
