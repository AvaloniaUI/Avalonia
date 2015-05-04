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
                new Style(x => x.OfType<Deck>().Descendent().Is<DeckItem>())
                {
                    Setters = new[]
                    {
                        new Setter(Control.IsVisibleProperty, false),
                    },
                },
                new Style(x => x.OfType<Deck>().Descendent().Is<DeckItem>().Class(":selected"))
                {
                    Setters = new[]
                    {
                        new Setter(Control.IsVisibleProperty, true),
                    },
                }
            });
        }

        private Control Template(Deck control)
        {
            return new ItemsPresenter
            {
                Id = "itemsPresenter",
                [~ItemsPresenter.ItemsProperty] = control[~Deck.ItemsProperty],
                [~ItemsPresenter.ItemsPanelProperty] = control[~Deck.ItemsPanelProperty],
                [~ItemsPresenter.MemberSelectorProperty] = control[~Deck.MemberSelectorProperty],
            };
        }
    }
}
