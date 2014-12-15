// -----------------------------------------------------------------------
// <copyright file="RadioButtonStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System;
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Controls.Shapes;
    using Perspex.Styling;
    using Perspex.Controls.Presenters;

    public class RadioButtonStyle : Styles
    {
        public RadioButtonStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<RadioButton>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty, ControlTemplate.Create<RadioButton>(this.Template)),
                    },
                },
                new Style(x => x.OfType<RadioButton>().Template().Id("checkMark"))
                {
                    Setters = new[]
                    {
                        new Setter(Shape.IsVisibleProperty, false),
                    },
                },
                new Style(x => x.OfType<RadioButton>().Class(":checked").Template().Id("checkMark"))
                {
                    Setters = new[]
                    {
                        new Setter(Shape.IsVisibleProperty, true),
                    },
                },
            });
        }

        private Control Template(RadioButton control)
        {
            Border result = new Border
            {
                [~Border.BackgroundProperty] = control[~RadioButton.BackgroundProperty],
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    },
                    Children = new Controls
                    {
                        new Ellipse
                        {
                            Id = "checkBorder",
                            Stroke = Brushes.Black,
                            StrokeThickness = 2,
                            Width = 18,
                            Height = 18,
                            VerticalAlignment = VerticalAlignment.Center,
                            [Grid.ColumnProperty] = 0,
                        },
                        new Ellipse
                        {
                            Id = "checkMark",
                            Fill = Brushes.Black,
                            Width = 10,
                            Height = 10,
                            Stretch = Stretch.Uniform,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            [Grid.ColumnProperty] = 0,
                        },
                        new ContentPresenter
                        {
                            Margin = new Thickness(4, 0, 0, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            [~ContentPresenter.ContentProperty] = control[~RadioButton.ContentProperty],
                            [Grid.ColumnProperty] = 1,
                        },
                    },
                },
            };

            return result;
        }
    }
}
