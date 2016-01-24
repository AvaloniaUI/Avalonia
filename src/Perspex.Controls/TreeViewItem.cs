// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls.Generators;
using Perspex.Controls.Mixins;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.LogicalTree;

namespace Perspex.Controls
{
    /// <summary>
    /// An item in a <see cref="TreeView"/>.
    /// </summary>
    public class TreeViewItem : HeaderedItemsControl, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="IsExpanded"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> IsExpandedProperty =
            PerspexProperty.Register<TreeViewItem, bool>("IsExpanded");

        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> IsSelectedProperty =
            ListBoxItem.IsSelectedProperty.AddOwner<TreeViewItem>();

        private static readonly ITemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel
            {
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
            });

        private TreeView _treeView;

        /// <summary>
        /// Initializes static members of the <see cref="TreeViewItem"/> class.
        /// </summary>
        static TreeViewItem()
        {
            SelectableMixin.Attach<TreeViewItem>(IsSelectedProperty);
            FocusableProperty.OverrideDefaultValue<TreeViewItem>(true);
            ItemsPanelProperty.OverrideDefaultValue<TreeViewItem>(DefaultPanel);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the item is expanded to show its children.
        /// </summary>
        public bool IsExpanded
        {
            get { return GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get { return GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        /// Gets the <see cref="ITreeItemContainerGenerator"/> for the tree view.
        /// </summary>
        public new ITreeItemContainerGenerator ItemContainerGenerator =>
            (ITreeItemContainerGenerator)base.ItemContainerGenerator;

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            var result =  new TreeItemContainerGenerator<TreeViewItem>(
                this,
                TreeViewItem.HeaderProperty,
                TreeViewItem.ItemsProperty,
                TreeViewItem.IsExpandedProperty,
                _treeView?.ItemContainerGenerator.Index ?? new TreeContainerIndex());

            result.Materialized += ItemMaterialized;

            return result;
        }

        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _treeView = this.GetLogicalAncestors().OfType<TreeView>().FirstOrDefault();
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            ItemContainerGenerator.Clear();
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                switch (e.Key)
                {
                    case Key.Right:
                        if (Items != null && Items.Cast<object>().Any())
                        {
                            IsExpanded = true;
                        }

                        e.Handled = true;
                        break;

                    case Key.Left:
                        IsExpanded = false;
                        e.Handled = true;
                        break;
                }
            }

            base.OnKeyDown(e);
        }

        private void ItemMaterialized(object sender, ItemContainerEventArgs e)
        {
            var selectedItem = _treeView?.SelectedItem;

            if (selectedItem != null)
            {
                foreach (var container in e.Containers)
                {
                    if (container.Item == selectedItem)
                    {
                        ((TreeViewItem)container.ContainerControl).IsSelected = true;
                    }
                }
            }
        }
    }
}
