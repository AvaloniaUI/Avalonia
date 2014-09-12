// -----------------------------------------------------------------------
// <copyright file="TabStripStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Media;
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
                        new Setter(Button.TemplateProperty, ControlTemplate.Create<TabStrip>(this.Template)),
                    },
                },
            });
        }

        private Control Template(TabStrip control)
        {
            return new ItemsPresenter
            {
                ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
                [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
            };
        }
    }
}
