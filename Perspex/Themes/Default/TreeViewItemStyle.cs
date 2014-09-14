// -----------------------------------------------------------------------
// <copyright file="TreeViewItemStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Media;
    using Perspex.Shapes;
    using Perspex.Styling;

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
                new Style(x => x.OfType<TreeViewItem>().Template().OfType<ToggleButton>().Class("expander"))
                {
                    Setters = new[]
                    {
                        new Setter(ToggleButton.TemplateProperty, ControlTemplate.Create<ToggleButton>(this.ToggleButtonTemplate)),
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
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children = new Controls
                        {
                            new ToggleButton
                            {
                                Classes = new Classes("expander"),
                            },
                            new ContentPresenter
                            {
                                [~ContentPresenter.ContentProperty] = control[~TreeViewItem.HeaderProperty],
                            },
                        }
                    },
                    new ItemsPresenter
                    {
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
            return new Path
            {
                Fill = Brushes.Black,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Width = 16,
                Height = 16,
                VerticalAlignment = Layout.VerticalAlignment.Center,
                Data = StreamGeometry.Parse("M 4 0 L 8 4 L 4 8 Z"),
            };
        }
    }
}
