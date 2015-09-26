// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
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
            AddRange(new[]
            {
                new Style(x => x.OfType<TreeView>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new ControlTemplate<TreeView>(Template)),
                        new Setter(TemplatedControl.BorderBrushProperty, Brushes.Black),
                        new Setter(TemplatedControl.BorderThicknessProperty, 1.0),
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
                [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~TemplatedControl.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~TemplatedControl.BorderThicknessProperty],
                Child = new ScrollViewer
                {
                    CanScrollHorizontally = true,
                    Content = new ItemsPresenter
                    {
                        Name = "itemsPresenter",
                        MemberSelector = control.MemberSelector,
                        [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                        [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                    }
                }
            };
        }
    }
}
