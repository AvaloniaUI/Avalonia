// -----------------------------------------------------------------------
// <copyright file="ButtonStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Media;
    using Perspex.Styling;

    public class ButtonStyle : Styles
    {
        public ButtonStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<Button>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty, ControlTemplate.Create<Button>(this.Template)),
                    },
                },
                new Style(x => x.OfType<Button>().Template().Id("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.BackgroundProperty, new SolidColorBrush(0xffdddddd)),
                        new Setter(Button.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(Button.BorderThicknessProperty, 2.0),
                        new Setter(Button.ForegroundProperty, new SolidColorBrush(0xff000000)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":pointerover").Template().Id("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.BackgroundProperty, new SolidColorBrush(0xffbee6fd)),
                        new Setter(Button.BorderBrushProperty, new SolidColorBrush(0xff3c7fb1)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":pointerover").Class(":pressed").Template().Id("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.BackgroundProperty, new SolidColorBrush(0xffc4e5f6)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":pressed").Template().Id("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.BorderBrushProperty, new SolidColorBrush(0xffff628b)),
                    },
                },
            });
        }

        private Control Template(Button control)
        {
            Border border = new Border
            {
                Id = "border",
                Padding = new Thickness(3),
                Content = new ContentPresenter
                {
                    [~ContentPresenter.ContentProperty] = control[~Button.ContentProperty],
                },
                [~Border.BackgroundProperty] = control[~Button.BackgroundProperty],
            };

            return border;
        }
    }
}
