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
                        Id = "tabStrip",
                        [~TabStrip.ItemsProperty] = control[~TabControl.ItemsProperty],
                        [~~TabStrip.SelectedTabProperty] = control[~~TabControl.SelectedTabProperty],
                    },
                    new ContentPresenter
                    {
                        Id = "contentPresenter",
                        [~ContentPresenter.ContentProperty] = control[~TabControl.SelectedContentProperty],
                        [Grid.RowProperty] = 1,
                    }
                }
            };
        }
    }
}
