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
    using Perspex.Controls.Templates;
    using Perspex.Input;
    using Perspex.Styling;

    /// <summary>
    /// The default style for the <see cref="Menu"/> control.
    /// </summary>
    public class MenuStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MenuStyle"/> class.
        /// </summary>
        public MenuStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<Menu>())
                {
                    Setters = new[]
                    {
                        new Setter(Menu.TemplateProperty, new ControlTemplate<Menu>(Template)),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="Menu"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(Menu control)
        {
            return new Border
            {
                [~Border.BackgroundProperty] = control[~Menu.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~Menu.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~Menu.BorderThicknessProperty],
                [~Border.PaddingProperty] = control[~Menu.PaddingProperty],
                Child = new ItemsPresenter
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
