// -----------------------------------------------------------------------
// <copyright file="DropDownStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Shapes;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Styling;

    public class DropDownStyle : Styles
    {
        public DropDownStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<DropDown>())
                {
                    Setters = new[]
                    {
                        new Setter(DropDown.TemplateProperty, ControlTemplate.Create<DropDown>(this.Template)),
                        new Setter(DropDown.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(DropDown.BorderThicknessProperty, 2.0),
                        new Setter(DropDown.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                        new Setter(DropDown.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                    },
                },
                new Style(x => x.OfType<DropDown>().Descendent().OfType<ListBoxItem>().Class(":pointerover"))
                {
                    Setters = new[]
                    {
                        new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush(0xffbee6fd))
                    }
                }
            });
        }

        private Control Template(DropDown control)
        {
            Border result = new Border
            {
                [~Border.BackgroundProperty] = control[~DropDown.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~DropDown.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~DropDown.BorderThicknessProperty],
                Content = new Grid
                {
                    Name = "container",
                    ColumnDefinitions = new ColumnDefinitions
                    {
                        new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                        new ColumnDefinition(GridLength.Auto),
                    },
                    Children = new Controls
                    {
                        new ContentControl
                        {
                            Name = "contentControl",
                            Margin = new Thickness(3),
                            [~ContentControl.ContentProperty] = control[~DropDown.ContentProperty],
                            [~ContentControl.HorizontalAlignmentProperty] = control[~DropDown.HorizontalContentAlignmentProperty],
                            [~ContentControl.VerticalAlignmentProperty] = control[~DropDown.VerticalContentAlignmentProperty],
                        },
                        new ToggleButton
                        {
                            Name = "toggle",
                            BorderThickness = 0,
                            Background = Brushes.Transparent,
                            ClickMode = ClickMode.Press,
                            Content = new Path
                            {
                                Name = "checkMark",
                                Fill = Brushes.Black,
                                Width = 8,
                                Height = 4,
                                Stretch = Stretch.Uniform,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Data = StreamGeometry.Parse("F1 M 301.14,-189.041L 311.57,-189.041L 306.355,-182.942L 301.14,-189.041 Z"),
                                [Grid.ColumnProperty] = 0,
                            },
                            [~~ToggleButton.IsCheckedProperty] = control[~~DropDown.IsDropDownOpenProperty],
                            [Grid.ColumnProperty] = 1,
                        },
                        new Popup
                        {
                            Child = new ListBox
                            {
                                [~ListBox.ItemsProperty] = control[~DropDown.ItemsProperty],
                                [~~ListBox.SelectedItemProperty] = control[~~DropDown.SelectedItemProperty],
                            },
                            PlacementTarget = control,
                            StaysOpen = false,
                            [~~Popup.IsOpenProperty] = control[~~DropDown.IsDropDownOpenProperty],
                            [~Popup.MinWidthProperty] = control[~DropDown.BoundsProperty].Cast<Rect>().Select(x => (object)x.Width),                       }
                    },
                },
            };

            return result;
        }
    }
}
