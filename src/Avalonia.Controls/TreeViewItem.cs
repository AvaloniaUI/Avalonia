// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// An item in a <see cref="TreeView"/>.
    /// </summary>
    public class TreeViewItem : HeaderedItemsControl, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="IsExpanded"/> property.
        /// </summary>
        public static readonly DirectProperty<TreeViewItem, bool> IsExpandedProperty =
            AvaloniaProperty.RegisterDirect<TreeViewItem, bool>(
                nameof(IsExpanded),
                o => o.IsExpanded,
                (o, v) => o.IsExpanded = v);

        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            ListBoxItem.IsSelectedProperty.AddOwner<TreeViewItem>();

        /// <summary>
        /// Defines the <see cref="Level"/> property.
        /// </summary>
        public static readonly DirectProperty<TreeViewItem, int> LevelProperty =
            AvaloniaProperty.RegisterDirect<TreeViewItem, int>(
                nameof(Level), o => o.Level);

        private static readonly ITemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel());

        private TreeView _treeView;
        private IControl _header;
        private bool _isExpanded;
        private int _level;

        /// <summary>
        /// Initializes static members of the <see cref="TreeViewItem"/> class.
        /// </summary>
        static TreeViewItem()
        {
            SelectableMixin.Attach<TreeViewItem>(IsSelectedProperty);
            FocusableProperty.OverrideDefaultValue<TreeViewItem>(true);
            ItemsPanelProperty.OverrideDefaultValue<TreeViewItem>(DefaultPanel);
            RequestBringIntoViewEvent.AddClassHandler<TreeViewItem>((x, e) => x.OnRequestBringIntoView(e));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the item is expanded to show its children.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { SetAndRaise(IsExpandedProperty, ref _isExpanded, value); }
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
        /// Gets the level/indentation of the item.
        /// </summary>
        public int Level
        {
            get { return _level; }
            private set { SetAndRaise(LevelProperty, ref _level, value); }
        }

        /// <summary>
        /// Gets the <see cref="ITreeItemContainerGenerator"/> for the tree view.
        /// </summary>
        public new ITreeItemContainerGenerator ItemContainerGenerator =>
            (ITreeItemContainerGenerator)base.ItemContainerGenerator;

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TreeItemContainerGenerator<TreeViewItem>(
                this,
                TreeViewItem.HeaderProperty,
                TreeViewItem.ItemTemplateProperty,
                TreeViewItem.ItemsProperty,
                TreeViewItem.IsExpandedProperty,
                _treeView?.ItemContainerGenerator.Index ?? new TreeContainerIndex());
        }

        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _treeView = this.GetLogicalAncestors().OfType<TreeView>().FirstOrDefault();

            Level = CalculateDistanceFromLogicalParent<TreeView>(this) - 1;

            if (ItemTemplate == null && _treeView?.ItemTemplate != null)
            {
                ItemTemplate = _treeView.ItemTemplate;
            }
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            ItemContainerGenerator.Clear();
        }

        protected virtual void OnRequestBringIntoView(RequestBringIntoViewEventArgs e)
        {
            if (e.TargetObject == this && _header != null)
            {
                var m = _header.TransformToVisual(this);

                if (m.HasValue)
                {
                    var bounds = new Rect(_header.Bounds.Size);
                    var rect = bounds.TransformToAABB(m.Value);
                    e.TargetRect = rect;
                }
            }
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

            // Don't call base.OnKeyDown - let events bubble up to containing TreeView.
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);
            _header = e.NameScope.Find<IControl>("PART_Header");
        }

        private static int CalculateDistanceFromLogicalParent<T>(ILogical logical, int @default = -1) where T : class
        {
            var result = 0;

            while (logical != null && !(logical is T))
            {
                ++result;
                logical = logical.LogicalParent;
            }

            return logical != null ? result : @default;
        }
    }
}
