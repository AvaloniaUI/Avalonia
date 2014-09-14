// -----------------------------------------------------------------------
// <copyright file="TreeViewStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Styling;

    public class TreeViewStyle : Styles
    {
        public TreeViewStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<TreeView>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty, ControlTemplate.Create<TreeView>(this.Template)),
                    },
                },
            });
        }

        private Control Template(TreeView control)
        {
            return new ItemsPresenter
            {
                [~ItemsPresenter.ItemsProperty] = control[~TreeView.ItemsProperty],
                [~ItemsPresenter.ItemsPanelProperty] = control[~TreeView.ItemsPanelProperty],
            };
        }
    }
}
