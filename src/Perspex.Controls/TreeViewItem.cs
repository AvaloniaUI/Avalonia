﻿// -----------------------------------------------------------------------
// <copyright file="TreeViewItem.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using Mixins;
    using Perspex.Controls.Generators;
    using Perspex.Controls.Primitives;
    using Perspex.Input;
    using Perspex.Rendering;
    using Perspex.VisualTree;

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

        private static readonly ItemsPanelTemplate DefaultPanel =
            new ItemsPanelTemplate(() => new StackPanel
            {
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Continue,
            });

        private TreeView treeView;

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
            get { return this.GetValue(IsExpandedProperty); }
            set { this.SetValue(IsExpandedProperty, value); }
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get { return this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            if (this.treeView == null)
            {
                throw new InvalidOperationException(
                    "Cannot get the ItemContainerGenerator for a TreeViewItem " +
                    "before it is added to a TreeView.");
            }

            return this.treeView.ItemContainerGenerator;
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);

            if (this.GetVisualParent() != null)
            {
                this.treeView = this.GetVisualAncestors().OfType<TreeView>().FirstOrDefault();

                if (this.treeView == null)
                {
                    throw new InvalidOperationException("TreeViewItems must be added to a TreeView.");
                }
            }
            else
            {
                this.treeView = null;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                switch (e.Key)
                {
                    case Key.Right:
                        if (this.Items != null && this.Items.Cast<object>().Any())
                        {
                            this.IsExpanded = true;
                        }

                        e.Handled = true;
                        break;

                    case Key.Left:
                        this.IsExpanded = false;
                        e.Handled = true;
                        break;
                }
            }

            base.OnKeyDown(e);
        }
    }
}
