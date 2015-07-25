// -----------------------------------------------------------------------
// <copyright file="TreeViewItemStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Controls.Shapes;
    using Perspex.Styling;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;

    public class TreeViewItemStyle : Styles
    {
        public TreeViewItemStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<TreeViewItem>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty, ControlTemplate.Create<TreeViewItem>(this.Template)),
                    },
                },
                new Style(x => x.OfType<TreeViewItem>().Template().Name("header"))
                {
                    Setters = new[]
                    {
                        new Setter(Border.PaddingProperty, new Thickness(2)),
                    },
                },
                new Style(x => x.OfType<TreeViewItem>().Class("selected").Template().Name("header"))
                {
                    Setters = new[]
                    {
                        new Setter(TreeViewItem.BackgroundProperty, new SolidColorBrush(0xff086f9e)),
                        new Setter(TreeViewItem.ForegroundProperty, Brushes.White),
                    },
                },
                new Style(x => x.OfType<TreeViewItem>().Template().OfType<ToggleButton>().Class("expander"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.TemplateProperty, ControlTemplate.Create<ToggleButton>(this.ToggleButtonTemplate)),
                    },
                },
                new Style(x => x.OfType<TreeViewItem>().Template().OfType<ToggleButton>().Class("expander").Class(":checked").Template().OfType<Path>())
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.RenderTransformProperty, new RotateTransform(45)),
                    },
                },
                new Style(x => x.OfType<TreeViewItem>().Class(":empty").Template().OfType<ToggleButton>().Class("expander"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.IsVisibleProperty, false),
                    },
                },
            });
        }

        private Control Template(TreeViewItem control)
        {
            return new StackPanel
            {
                Children = new Controls
                {
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions
                        {
                            new ColumnDefinition(new GridLength(16, GridUnitType.Pixel)),
                            new ColumnDefinition(GridLength.Auto),
                        },
                        Children = new Controls
                        {
                            new ToggleButton
                            {
                                Classes = new Classes("expander"),
                                [~~ToggleButton.IsCheckedProperty] = control[~TreeViewItem.IsExpandedProperty],
                            },
                            new Border
                            {
                                Name = "header",
                                [~Border.BackgroundProperty] = control[~TreeViewItem.BackgroundProperty],
                                [Grid.ColumnProperty] = 1,
                                Child = new ContentPresenter
                                {
                                    [~ContentPresenter.ContentProperty] = control[~TreeViewItem.HeaderProperty],
                                },
                            }
                        }
                    },
                    new ItemsPresenter
                    {
                        Name = "itemsPresenter",
                        Margin = new Thickness(24, 0, 0, 0),
                        [~ItemsPresenter.ItemsProperty] = control[~TreeViewItem.ItemsProperty],
                        [~ItemsPresenter.ItemsPanelProperty] = control[~TreeViewItem.ItemsPanelProperty],
                        [~ItemsPresenter.IsVisibleProperty] = control[~TreeViewItem.IsExpandedProperty],
                    }
                }
            };
        }

        private Control ToggleButtonTemplate(ToggleButton control)
        {
            return new Border
            {
                Width = 12,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new Path
                {
                    Fill = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = StreamGeometry.Parse("M 0 2 L 4 6 L 0 10 Z"),
                }
            };
        }
    }
}
