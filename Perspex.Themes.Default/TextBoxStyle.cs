// -----------------------------------------------------------------------
// <copyright file="TextBoxStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System;
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Media;
    using Perspex.Controls.Shapes;
    using Perspex.Styling;
    using Perspex.Controls.Presenters;

    public class TextBoxStyle : Styles
    {
        public TextBoxStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<TextBox>())
                {
                    Setters = new[]
                    {
                        new Setter(TextBox.TemplateProperty, ControlTemplate.Create<TextBox>(this.Template)),
                        new Setter(TextBox.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(TextBox.BorderThicknessProperty, 2.0),
                        new Setter(TextBox.FocusAdornerProperty, null),
                    },
                },
                new Style(x => x.OfType<TextBox>().Class(":focus").Template().Id("border"))
                {
                    Setters = new[]
                    {
                        new Setter(TextBox.BorderBrushProperty, Brushes.Black),
                    },
                }
            });
        }

        private Control Template(TextBox control)
        {
            Border result = new Border
            {
                Id = "border",
                Padding = new Thickness(2),
                [~Border.BackgroundProperty] = control[~TextBox.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~TextBox.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~TextBox.BorderThicknessProperty],
                Content = new ScrollViewer
                {
                    [~ScrollViewer.CanScrollHorizontallyProperty] = control[~ScrollViewer.CanScrollHorizontallyProperty],
                    [~ScrollViewer.HorizontalScrollBarVisibilityProperty] = control[~ScrollViewer.HorizontalScrollBarVisibilityProperty],
                    [~ScrollViewer.VerticalScrollBarVisibilityProperty] = control[~ScrollViewer.VerticalScrollBarVisibilityProperty],
                    Content = new TextPresenter
                    {
                        Id = "textPresenter",
                        [~TextPresenter.CaretIndexProperty] = control[~TextBox.CaretIndexProperty],
                        [~TextPresenter.SelectionStartProperty] = control[~TextBox.SelectionStartProperty],
                        [~TextPresenter.SelectionEndProperty] = control[~TextBox.SelectionEndProperty],
                        [~TextPresenter.TextProperty] = control[~TextBox.TextProperty],
                        [~TextPresenter.TextWrappingProperty] = control[~TextBox.TextWrappingProperty],
                    }
                }
            };

            return result;
        }
    }
}
