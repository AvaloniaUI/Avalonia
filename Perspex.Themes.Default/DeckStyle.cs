// -----------------------------------------------------------------------
// <copyright file="DeckStyle.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Styling;
    using System.Linq;

    public class DeckStyle : Styles
    {
        public DeckStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<Deck>())
                {
                    Setters = new[]
                    {
                        new Setter(Deck.TemplateProperty, ControlTemplate.Create<Deck>(this.Template)),
                    },
                },
            });
        }

        private Control Template(Deck control)
        {
            return new DeckPresenter
            {
                Id = "itemsPresenter",
                [~ItemsPresenter.ItemsProperty] = control[~Deck.ItemsProperty],
                [~ItemsPresenter.ItemsPanelProperty] = control[~Deck.ItemsPanelProperty],
                [~DeckPresenter.SelectedItemProperty] = control[~Deck.SelectedItemProperty],
            };
        }
    }
}
