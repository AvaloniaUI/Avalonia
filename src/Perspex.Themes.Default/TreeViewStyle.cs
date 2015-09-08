// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Perspex.Media;
using Perspex.Styling;

namespace Perspex.Themes.Default
{
    /// <summary>
    /// The default style for the <see cref="TreeView"/> control.
    /// </summary>
    public class TreeViewStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeViewStyle"/> class.
        /// </summary>
        public TreeViewStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<TreeView>())
                {
                    Setters = new[]
                    {
                        new Setter(TreeView.TemplateProperty, new ControlTemplate<TreeView>(Template)),
                        new Setter(TreeView.BorderBrushProperty, Brushes.Black),
                        new Setter(TreeView.BorderThicknessProperty, 1.0),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="TreeView"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(TreeView control)
        {
            return new Border
            {
                Padding = new Thickness(4),
                [~Border.BackgroundProperty] = control[~TreeView.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~TreeView.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~TreeView.BorderThicknessProperty],
                Child = new ScrollViewer
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
