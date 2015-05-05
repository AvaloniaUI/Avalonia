// -----------------------------------------------------------------------
// <copyright file="ItemsControlStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Styling;

    public class ItemsControlStyle : Styles
    {
        public ItemsControlStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ItemsControl>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty, ControlTemplate.Create<ItemsControl>(this.Template)),
                    },
                },
            });
        }

        private Control Template(ItemsControl control)
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
