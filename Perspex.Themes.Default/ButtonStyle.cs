// -----------------------------------------------------------------------
// <copyright file="ButtonStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Collections;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Shapes;
    using Perspex.Layout;
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
                        new Setter(Button.FocusAdornerProperty, new AdornerTemplate(FocusAdornerTemplate)),
                        new Setter(Button.TemplateProperty, ControlTemplate.Create<Button>(this.Template)),
                        new Setter(Button.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                        new Setter(Button.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                    },
                },
                new Style(x => x.OfType<Button>().Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.BackgroundProperty, new SolidColorBrush(0xffdddddd)),
                        new Setter(Button.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(Button.BorderThicknessProperty, 2.0),
                        new Setter(Button.ForegroundProperty, new SolidColorBrush(0xff000000)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":pointerover").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.BackgroundProperty, new SolidColorBrush(0xffbee6fd)),
                        new Setter(Button.BorderBrushProperty, new SolidColorBrush(0xff3c7fb1)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":pointerover").Class(":pressed").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.BackgroundProperty, new SolidColorBrush(0xffc4e5f6)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":pressed").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.BorderBrushProperty, new SolidColorBrush(0xffff628b)),
                    },
                },
                new Style(x => x.OfType<Button>().Class(":disabled").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(Button.ForegroundProperty, new SolidColorBrush(0xff7f7f7f)),
                    },
                },
            });
        }

        public static Control FocusAdornerTemplate()
        {
            return new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                StrokeDashArray = new PerspexList<double>(1, 2),
                Margin = new Thickness(3.5),
            };
        }

        private Control Template(Button control)
        {
            Border border = new Border
            {
                Name = "border",
                Padding = new Thickness(3),
                Content = new ContentPresenter
                {
                    Name = "contentPresenter",
                    [~ContentPresenter.ContentProperty] = control[~Button.ContentProperty],
                    [~ContentPresenter.HorizontalAlignmentProperty] = control[~Button.HorizontalContentAlignmentProperty],
                    [~ContentPresenter.VerticalAlignmentProperty] = control[~Button.VerticalContentAlignmentProperty],
                },
                [~Border.BackgroundProperty] = control[~Button.BackgroundProperty],
            };

            return border;
        }
    }
}
