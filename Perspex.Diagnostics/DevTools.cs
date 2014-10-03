// -----------------------------------------------------------------------
// <copyright file="DevTools.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    using Perspex.Controls;
    using System.Reactive.Linq;
    using Perspex.Diagnostics.ViewModels;
    using System;

    public class DevTools : Decorator
    {
        public static readonly PerspexProperty<Control> RootProperty =
            PerspexProperty.Register<DevTools, Control>("Root");

        public DevTools()
        {
            var treeView = new TreeView
            {
                DataTemplates = new DataTemplates
                {
                    new TreeDataTemplate<VisualTreeNode>(GetHeader, x => x.Children),
                },
                [!TreeView.ItemsProperty] = this[!DevTools.RootProperty].Select(x =>
                {
                    if (x != null)
                    {
                        return new[] { new VisualTreeNode((IVisual)x) };
                    }
                    else
                    {
                        return null;
                    }
                }),
            };

            var detailsView = new ContentControl
            {
                DataTemplates = new DataTemplates
                {
                    new DataTemplate<ControlDetails>(CreateDetailsView),
                },
                [!ContentControl.ContentProperty] = treeView[!TreeView.SelectedItemProperty]
                    .Where(x => x != null)
                    .Cast<VisualTreeNode>()
                    .Select(x => new ControlDetails(x.Visual)),
                [Grid.ColumnProperty] = 2,
            };

            var splitter = new GridSplitter
            {
                [Grid.ColumnProperty] = 1,
                Width = 4,
            };

            this.Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition(1, GridUnitType.Star),
                    new ColumnDefinition(4, GridUnitType.Pixel),
                    new ColumnDefinition(3, GridUnitType.Star),
                },
                Children = new Controls
                {
                    treeView,
                    splitter,
                    detailsView,
                }
            };
        }

        public Control Root
        {
            get { return this.GetValue(RootProperty); }
            set { this.SetValue(RootProperty, value); }
        }

        private static Control CreateDetailsView(ControlDetails i)
        {
            return new ItemsControl
            {
                DataTemplates = new DataTemplates
                {
                    new DataTemplate<PropertyDetails>(x =>
                        new StackPanel
                        {
                            Gap = 16,
                            Orientation = Orientation.Horizontal,
                            Children = new Controls
                            {
                                new TextBlock { Text = x.Name },
                                new TextBlock { Text = Convert.ToString(x.Value) },
                                new TextBlock { Text = x.Priority },
                            },
                        }),
                },
                Items = i.Properties,
            };
        }

        private static Control GetHeader(VisualTreeNode node)
        {
            TextBlock result = new TextBlock();
            result.Text = node.Type;

            if (node.IsInTemplate)
            {
                result.FontStyle = Media.FontStyle.Italic;
            }

            return result;
        }
    }
}
