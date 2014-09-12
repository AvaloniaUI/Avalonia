// -----------------------------------------------------------------------
// <copyright file="TabStripStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Styling;

    public class TabStripStyle : Styles
    {
        public TabStripStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<TabStrip>())
                {
                    Setters = new[]
                    {
                        new Setter(TabStrip.TemplateProperty, ControlTemplate.Create<TabStrip>(this.Template)),
                    },
                },
                new Style(x => x.OfType<TabStrip>().Template().OfType<StackPanel>())
                {
                    Setters = new[]
                    {
                        new Setter(StackPanel.OrientationProperty, Orientation.Horizontal),
                    },
                },
            });
        }

        private Control Template(TabStrip control)
        {
            return new ItemsPresenter
            {
                [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                [~ItemsPresenter.ItemTemplateProperty] = control[~ItemsControl.ItemTemplateProperty],
            };
        }
    }
}
