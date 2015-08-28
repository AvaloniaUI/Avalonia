﻿// -----------------------------------------------------------------------
// <copyright file="ListBoxItemStyle.cs" company="Steven Kirk">
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

    public class ListBoxItemStyle : Styles
    {
        public ListBoxItemStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ListBoxItem>())
                {
                    Setters = new[]
                    {
                        new Setter(ListBoxItem.TemplateProperty, new ControlTemplate<ListBoxItem>(this.Template)),
                    },
                },
                new Style(x => x.OfType<ListBoxItem>().Class("selected").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush(0xfff0f0f0)),
                    },
                },
                new Style(x => x.OfType<ListBoxItem>().Class("selected").Class(":focus").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush(0xff086f9e)),
                        new Setter(ListBoxItem.ForegroundProperty, Brushes.White),
                    },
                },
            });
        }

        private Control Template(ListBoxItem control)
        {
            return new Border
            {
                Name = "border",
                [~Border.BackgroundProperty] = control[~ListBoxItem.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~ListBoxItem.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~ListBoxItem.BorderThicknessProperty],
                Child = new ContentPresenter
                {
                    [~ContentPresenter.ContentProperty] = control[~ListBoxItem.ContentProperty],
                },
            };
        }
    }
}
