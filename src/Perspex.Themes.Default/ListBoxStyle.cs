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
    /// The default style for the <see cref="ListBox"/> control.
    /// </summary>
    public class ListBoxStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListBoxStyle"/> class.
        /// </summary>
        public ListBoxStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ListBox>())
                {
                    Setters = new[]
                    {
                        new Setter(ListBox.TemplateProperty, new ControlTemplate<ListBox>(Template)),
                        new Setter(ListBox.BorderBrushProperty, Brushes.Black),
                        new Setter(ListBox.BorderThicknessProperty, 1.0),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for a <see cref="ListBox"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(ListBox control)
        {
            return new Border
            {
                Padding = new Thickness(4),
                [~Border.BackgroundProperty] = control[~ListBox.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~ListBox.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~ListBox.BorderThicknessProperty],
                Child = new ScrollViewer
                {
                    Content = new ItemsPresenter
                    {
                        Name = "itemsPresenter",
                        [~ItemsPresenter.ItemsProperty] = control[~ListBox.ItemsProperty],
                        [~ItemsPresenter.ItemsPanelProperty] = control[~ListBox.ItemsPanelProperty],
                    }
                }
            };
        }
    }
}
