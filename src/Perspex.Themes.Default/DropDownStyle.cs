// -----------------------------------------------------------------------
// <copyright file="DropDownStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using System.Reactive.Linq;
    using Collections;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Shapes;
    using Perspex.Controls.Templates;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Styling;

    /// <summary>
    /// The default style for the <see cref="DropDown"/> control.
    /// </summary>
    public class DropDownStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DropDownStyle"/> class.
        /// </summary>
        public DropDownStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<DropDown>())
                {
                    Setters = new[]
                    {
                        new Setter(DropDown.TemplateProperty, new ControlTemplate<DropDown>(Template)),
                        new Setter(DropDown.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                        new Setter(DropDown.BorderThicknessProperty, 2.0),
                        new Setter(DropDown.FocusAdornerProperty, new FuncTemplate<IControl>(FocusAdornerTemplate)),
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

        /// <summary>
        /// The default template for a <see cref="DropDown"/> control's focus adorner.
        /// </summary>
        /// <returns>The root of the instantiated template.</returns>
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

        /// <summary>
        /// The default template for a <see cref="DropDown"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(DropDown control)
        {
            Border result = new Border
            {
                [~Border.BackgroundProperty] = control[~DropDown.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~DropDown.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~DropDown.BorderThicknessProperty],
                Child = new Grid
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
                            [~ContentControl.ContentProperty] = control[~DropDown.SelectionBoxItemProperty],
                            [~ContentControl.HorizontalAlignmentProperty] = control[~DropDown.HorizontalContentAlignmentProperty],
                            [~ContentControl.VerticalAlignmentProperty] = control[~DropDown.VerticalContentAlignmentProperty],
                        },
                        new ToggleButton
                        {
                            Name = "toggle",
                            BorderThickness = 0,
                            Background = Brushes.Transparent,
                            ClickMode = ClickMode.Press,
                            Focusable = false,
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
                            Name = "popup",
                            Child = new Border
                            {
                                BorderBrush = Brushes.Black,
                                BorderThickness = 1,
                                Padding = new Thickness(4),
                                Child = new ItemsPresenter
                                {
                                    [~ItemsPresenter.ItemsProperty] = control[~DropDown.ItemsProperty],
                                }
                            },
                            PlacementTarget = control,
                            StaysOpen = false,
                            [~~Popup.IsOpenProperty] = control[~~DropDown.IsDropDownOpenProperty],
                            [~Popup.MinWidthProperty] = control[~DropDown.BoundsProperty].Cast<Rect>().Select(x => (object)x.Width),
                        }
                    },
                },
            };

            return result;
        }
    }
}
