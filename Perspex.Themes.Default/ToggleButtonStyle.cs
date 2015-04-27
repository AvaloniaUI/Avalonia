// -----------------------------------------------------------------------
// <copyright file="ToggleButtonStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Styling;

    public class ToggleButtonStyle : Styles
    {
        public ToggleButtonStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ToggleButton>())
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.TemplateProperty, ControlTemplate.Create<ToggleButton>(this.Template)),
                        new Setter(ToggleButton.BackgroundProperty, new SolidColorBrush(0xffdddddd)),
                        new Setter(ToggleButton.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(ToggleButton.BorderThicknessProperty, 2.0),
                        new Setter(ToggleButton.FocusAdornerProperty, new AdornerTemplate(ButtonStyle.FocusAdornerTemplate)),
                        new Setter(ToggleButton.ForegroundProperty, new SolidColorBrush(0xff000000)),
                        new Setter(ToggleButton.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                        new Setter(ToggleButton.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":checked").Template().Id("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.BackgroundProperty, new SolidColorBrush(0xff7f7f7f)),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":pointerover").Template().Id("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.BackgroundProperty, new SolidColorBrush(0xffbee6fd)),
                        new Setter(ToggleButton.BorderBrushProperty, new SolidColorBrush(0xff3c7fb1)),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":checked").Class(":pointerover").Template().Id("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.BackgroundProperty, new SolidColorBrush(0xffa0a0a0)),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":pointerover").Class(":pressed").Template().Id("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.BackgroundProperty, new SolidColorBrush(0xffc4e5f6)),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":pressed").Template().Id("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.BorderBrushProperty, new SolidColorBrush(0xffff628b)),
                    },
                },
                new Style(x => x.OfType<ToggleButton>().Class(":disabled").Template().Id("border"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.ForegroundProperty, new SolidColorBrush(0xff7f7f7f)),
                    },
                },
            });
        }

        private Control Template(ToggleButton control)
        {
            Border border = new Border
            {
                Id = "border",
                Padding = new Thickness(3),
                [~Border.BackgroundProperty] = control[~ToggleButton.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~ToggleButton.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~ToggleButton.BorderThicknessProperty],
                Content = new ContentPresenter
                {
                    Id = "contentPresenter",
                    [~ContentPresenter.ContentProperty] = control[~ToggleButton.ContentProperty],
                    [~ContentPresenter.HorizontalAlignmentProperty] = control[~ToggleButton.HorizontalContentAlignmentProperty],
                    [~ContentPresenter.VerticalAlignmentProperty] = control[~ToggleButton.VerticalContentAlignmentProperty],
                },
            };

            return border;
        }
    }
}
