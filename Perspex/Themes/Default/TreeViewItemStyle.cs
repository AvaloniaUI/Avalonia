// -----------------------------------------------------------------------
// <copyright file="TreeViewItemStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Styling;

    public class TreeViewItemStyle : Styles
    {
        public TreeViewItemStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<TreeViewItem>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty, ControlTemplate.Create<TreeViewItem>(this.Template)),
                    },
                },
            });
        }

        private Control Template(TreeViewItem control)
        {
            return new StackPanel
            {
                Children = new Controls
                {
                    new ContentPresenter
                    {
                        [~ContentPresenter.ContentProperty] = control[~TreeViewItem.HeaderProperty],
                    },
                    new ItemsPresenter
                    {
                        Margin = new Thickness(13, 0, 0, 0),
                        [~ItemsPresenter.ItemsProperty] = control[~TreeViewItem.ItemsProperty],
                        [~ItemsPresenter.ItemsPanelProperty] = control[~TreeViewItem.ItemsPanelProperty],
                    }
                }
            };
        }
    }
}
