// -----------------------------------------------------------------------
// <copyright file="DeckItemStyle.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Styling;
    using System.Linq;

    public class DeckItemStyle : Styles
    {
        public DeckItemStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<DeckItem>())
                {
                    Setters = new[]
                    {
                        new Setter(DeckItem.TemplateProperty, ControlTemplate.Create<DeckItem>(this.Template)),
                    },
                },
            });
        }

        private Control Template(DeckItem control)
        {
            return new ContentPresenter
            {
                [~ContentPresenter.ContentProperty] = control[~DeckItem.ContentProperty],
            };
        }
    }
}
