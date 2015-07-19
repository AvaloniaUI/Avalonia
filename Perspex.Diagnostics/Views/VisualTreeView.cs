// -----------------------------------------------------------------------
// <copyright file="VisualTreeView.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.Views
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using Perspex.Diagnostics.ViewModels;
    using Perspex.Media;
    using ReactiveUI;

    internal class VisualTreeView : TreePage
    {
        private static readonly PerspexProperty<VisualTreeViewModel> ViewModelProperty =
            PerspexProperty.Register<VisualTreeView, VisualTreeViewModel>("ViewModel");

        public VisualTreeView()
        {
            this.InitializeComponent();
            this.GetObservable(DataContextProperty)
                .Subscribe(x => this.ViewModel = (VisualTreeViewModel)x);
        }

        public VisualTreeViewModel ViewModel
        {
            get { return this.GetValue(ViewModelProperty); }
            private set { this.SetValue(ViewModelProperty, value); }
        }

        private void InitializeComponent()
        {
            TreeView tree;

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
                    (tree = new TreeView
                    {
                        DataTemplates = new DataTemplates
                        {
                            new TreeDataTemplate<VisualTreeNode>(this.GetHeader, x => x.Children),
                        },
                        [!TreeView.ItemsProperty] = this.WhenAnyValue(x => x.ViewModel.Nodes),
                    }),
                    new GridSplitter
                    {
                        [Grid.ColumnProperty] = 1,
                        Width = 4,
                    },
                    new ContentControl
                    {
                        [!ContentControl.ContentProperty] = this.WhenAnyValue(x => x.ViewModel.Details),
                        [Grid.ColumnProperty] = 2,
                    }
                }
            };

            tree.GetObservable(TreeView.SelectedItemProperty)
                .OfType<VisualTreeNode>()
                .Subscribe(x => this.ViewModel.SelectedNode = x);
        }

        private Control GetHeader(VisualTreeNode node)
        {
            var result = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Gap = 8,
                Children = new Controls
                {
                    new TextBlock
                    {
                        FontStyle = node.IsInTemplate ? FontStyle.Italic : FontStyle.Normal,
                        Text = node.Type,
                    },
                    new TextBlock
                    {
                        [!TextBlock.TextProperty] = node.WhenAnyValue(x => x.Classes),
                    }
                }
            };

            result.PointerEnter += this.AddAdorner;
            result.PointerLeave += this.RemoveAdorner;

            return result;
        }
    }
}
