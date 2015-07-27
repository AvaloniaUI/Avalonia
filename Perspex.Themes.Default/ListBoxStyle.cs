// -----------------------------------------------------------------------
// <copyright file="ListBoxStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Templates;
    using Perspex.Media;
    using Perspex.Styling;

    public class ListBoxStyle : Styles
    {
        public ListBoxStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ListBox>())
                {
                    Setters = new[]
                    {
                        new Setter(ListBox.TemplateProperty, new ControlTemplate<ListBox>(this.Template)),
                        new Setter(ListBox.BorderBrushProperty, Brushes.Black),
                        new Setter(ListBox.BorderThicknessProperty, 1.0),
                    },
                },
            });
        }

        private Control Template(ListBox control)
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
