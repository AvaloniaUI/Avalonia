// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Perspex.Controls;
using Perspex.Controls.Templates;
using Perspex.Diagnostics.ViewModels;
using ReactiveUI;

namespace Perspex.Diagnostics.Views
{
    using Controls = Controls.Controls;

    internal class LogicalTreeView : TreePage
    {
        private static readonly PerspexProperty<LogicalTreeViewModel> ViewModelProperty =
            PerspexProperty.Register<LogicalTreeView, LogicalTreeViewModel>("ViewModel");

        public LogicalTreeView()
        {
            InitializeComponent();
            GetObservable(DataContextProperty)
                .Subscribe(x => ViewModel = (LogicalTreeViewModel)x);
        }

        public LogicalTreeViewModel ViewModel
        {
            get { return GetValue(ViewModelProperty); }
            private set { SetValue(ViewModelProperty, value); }
        }

        private void InitializeComponent()
        {
            TreeView tree;

            Content = new Grid
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
                            new FuncTreeDataTemplate<LogicalTreeNode>(GetHeader, x => x.Children),
                        },
                        [!ItemsControl.ItemsProperty] = this.WhenAnyValue(x => x.ViewModel.Nodes),
                    }),
                    new GridSplitter
                    {
                        [Grid.ColumnProperty] = 1,
                        Width = 4,
                    },
                    new ContentControl
                    {
                        [!ContentProperty] = this.WhenAnyValue(x => x.ViewModel.Details),
                        [Grid.ColumnProperty] = 2,
                    }
                }
            };

            tree.GetObservable(TreeView.SelectedItemProperty)
                .OfType<LogicalTreeNode>()
                .Subscribe(x => ViewModel.SelectedNode = x);
        }

        private Control GetHeader(LogicalTreeNode node)
        {
            var result = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Gap = 8,
                Children = new Controls
                {
                    new TextBlock
                    {
                        Text = node.Type,
                    },
                    new TextBlock
                    {
                        [!TextBlock.TextProperty] = node.WhenAnyValue(x => x.Classes),
                    }
                }
            };

            result.PointerEnter += AddAdorner;
            result.PointerLeave += RemoveAdorner;

            return result;
        }
    }
}
