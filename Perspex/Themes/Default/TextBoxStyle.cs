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
    using Perspex.Shapes;
    using Perspex.Styling;

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
                Content = new Decorator
                {
                    Id = "textContainer",
                }
            };

            return result;
        }
    }
}
