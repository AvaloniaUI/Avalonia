// -----------------------------------------------------------------------
// <copyright file="TreeViewStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Media;
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
                        new Setter(TreeView.TemplateProperty, ControlTemplate.Create<TreeView>(this.Template)),
                        new Setter(TreeView.BorderBrushProperty, Brushes.Black),
                        new Setter(TreeView.BorderThicknessProperty, 1.0),
                    },
                },
            });
        }

        private Control Template(TreeView control)
        {
            return new Border
            {
                Padding = new Thickness(4),
                [~Border.BackgroundProperty] = control[~TreeView.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~TreeView.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~TreeView.BorderThicknessProperty],
                Content = new ScrollViewer
                {
                    CanScrollHorizontally = true,
                    Content = new ItemsPresenter
                    {
                        Name = "itemsPresenter",
                        [~ItemsPresenter.ItemsProperty] = control[~TreeView.ItemsProperty],
                        [~ItemsPresenter.ItemsPanelProperty] = control[~TreeView.ItemsPanelProperty],
                    }
                }
            };
        }
    }
}
