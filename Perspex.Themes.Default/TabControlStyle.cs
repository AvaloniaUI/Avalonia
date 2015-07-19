// -----------------------------------------------------------------------
// <copyright file="TabControlStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Styling;
    using Perspex.Controls.Templates;
    using Perspex.Animation;
    using System;

    public class TabControlStyle : Styles
    {
        public TabControlStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<TabControl>())
                {
                    Setters = new[]
                    {
                        new Setter(TabControl.TemplateProperty, ControlTemplate.Create<TabControl>(this.Template)),
                    },
                },
            });
        }

        private Control Template(TabControl control)
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
                        [~TabStrip.ItemsProperty] = control[~TabControl.ItemsProperty],
                        [!!TabStrip.SelectedItemProperty] = control[!!TabControl.SelectedItemProperty],
                    },
                    new Deck
                    {
                        Name = "deck",
                        DataTemplates = new DataTemplates
                        {
                            new DataTemplate<TabItem>(x => control.MaterializeDataTemplate(x.Content)),
                        },
                        [~Deck.ItemsProperty] = control[~TabControl.ItemsProperty],
                        [!Deck.SelectedItemProperty] = control[!TabControl.SelectedItemProperty],
                        [~Deck.TransitionProperty] = control[~TabControl.TransitionProperty],
                        [Grid.RowProperty] = 1,
                    }
                }
            };
        }
    }
}
