// -----------------------------------------------------------------------
// <copyright file="MenuStyle.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Input;
    using Perspex.Styling;

    public class MenuStyle : Styles
    {
        public MenuStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<Menu>())
                {
                    Setters = new[]
                    {
                        new Setter(Menu.TemplateProperty, ControlTemplate.Create<Menu>(this.Template)),
                    },
                },
            });
        }

        private Control Template(Menu control)
        {
            return new Border
            {
                [~Border.BackgroundProperty] = control[~Menu.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~Menu.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~Menu.BorderThicknessProperty],
                [~Border.PaddingProperty] = control[~Menu.PaddingProperty],
                Content = new ItemsPresenter
                {
                    Name = "itemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = control[~Menu.ItemsProperty],
                    [~ItemsPresenter.ItemsPanelProperty] = control[~Menu.ItemsPanelProperty],
                    [KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Continue,
                }
            };
        }
    }
}
