// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Reactive.Linq;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Styling;

namespace Perspex.Themes.Default
{
    using Controls = Controls.Controls;

    /// <summary>
    /// The default style for the <see cref="TabControl"/> control.
    /// </summary>
    public class TabControlStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TabControlStyle"/> class.
        /// </summary>
        public TabControlStyle()
        {
            AddRange(new[]
            {
                new Style(x => x.OfType<TabControl>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new ControlTemplate<TabControl>(Template)),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="TabControl"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(TabControl control)
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
                        [!ItemsControl.ItemsProperty] = control[!ItemsControl.ItemsProperty],
                        [!!SelectingItemsControl.SelectedItemProperty] = control[!!SelectingItemsControl.SelectedItemProperty],
                    },
                    new Deck
                    {
                        Name = "deck",
                        MemberSelector = new FuncMemberSelector<TabItem, object>(x => x.Content),
                        [!Deck.ItemsProperty] = control[!ItemsControl.ItemsProperty],
                        [!Deck.SelectedItemProperty] = control[!SelectingItemsControl.SelectedItemProperty],
                        [~Deck.TransitionProperty] = control[~TabControl.TransitionProperty],
                        [Grid.RowProperty] = 1,
                    }
                }
            };
        }
    }
}
