using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// An item in a <see cref="TreeView"/>.
    /// </summary>
    [TemplatePart("PART_Header", typeof(Control))]
    [PseudoClasses(":pressed", ":selected")]
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

        private static readonly ITemplate<Panel> DefaultPanel =
            new FuncTemplate<Panel>(() => new StackPanel());

        private TreeView? _treeView;
        private Control? _header;
        private bool _isExpanded;
        private int _level;
        private bool _templateApplied;
        private bool _deferredBringIntoViewFlag;

        /// <summary>
        /// Initializes static members of the <see cref="TreeViewItem"/> class.
        /// </summary>
        static TreeViewItem()
        {
            SelectableMixin.Attach<TreeViewItem>(IsSelectedProperty);
            PressedMixin.Attach<TreeViewItem>();
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

        internal TreeView? TreeViewOwner => _treeView;

        protected internal override Control CreateContainerForItemOverride() => new TreeViewItem();
        protected internal override bool IsItemItsOwnContainerOverride(Control item) => item is TreeViewItem;

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

            if (ItemContainerTheme == null && _treeView?.ItemContainerTheme != null)
            {
                ItemContainerTheme = _treeView.ItemContainerTheme;
            }
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
        }

        protected virtual void OnRequestBringIntoView(RequestBringIntoViewEventArgs e)
        {
            if (e.TargetObject == this)
            {
                if (!_templateApplied)
                {
                    _deferredBringIntoViewFlag = true;
                    return;
                }

                if (_header != null)
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
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                Func<TreeViewItem, bool>? handler =
                    e.Key switch
                    {
                        Key.Left => ApplyToItemOrRecursivelyIfCtrl(FocusAwareCollapseItem, e.KeyModifiers),
                        Key.Right => ApplyToItemOrRecursivelyIfCtrl(ExpandItem, e.KeyModifiers),
                        Key.Enter or Key.Space => ApplyToItemOrRecursivelyIfCtrl(IsExpanded ? CollapseItem : ExpandItem, e.KeyModifiers),

                        // do not handle CTRL with numpad keys
                        Key.Subtract => FocusAwareCollapseItem,
                        Key.Add => ExpandItem,
                        Key.Divide => ApplyToSubtree(CollapseItem),
                        Key.Multiply => ApplyToSubtree(ExpandItem),
                        _ => null,
                    };

                if (handler is not null)
                {
                    e.Handled = handler(this);
                }

                // NOTE: these local functions do not use the TreeView.Expand/CollapseSubtree
                // function because we want to know if any items were in fact expanded to set the
                // event handled status. Also the handling here avoids a potential infinite recursion/stack overflow.
                static Func<TreeViewItem, bool> ApplyToSubtree(Func<TreeViewItem, bool> f)
                {
                    // Calling toList enumerates all items before applying functions. This avoids a
                    // potential infinite loop if there is an infinite tree (the control catalog is
                    // lazily infinite). But also means a lazily loaded tree will not be expanded completely.
                    return t => SubTree(t)
                        .ToList()
                        .Select(treeViewItem => f(treeViewItem))
                        .Aggregate(false, (p, c) => p || c);
                }

                static Func<TreeViewItem, bool> ApplyToItemOrRecursivelyIfCtrl(Func<TreeViewItem,bool> f, KeyModifiers keyModifiers)
                {
                    if (keyModifiers.HasAllFlags(KeyModifiers.Control))
                    {
                        return ApplyToSubtree(f);
                    }

                    return f;
                }

                static bool ExpandItem(TreeViewItem treeViewItem)
                {
                    if (treeViewItem.ItemCount > 0 && !treeViewItem.IsExpanded)
                    {
                        treeViewItem.IsExpanded = true;
                        return true;
                    }

                    return false;
                }

                static bool CollapseItem(TreeViewItem treeViewItem)
                {
                    if (treeViewItem.ItemCount > 0 && treeViewItem.IsExpanded)
                    {
                        treeViewItem.IsExpanded = false;
                        return true;
                    }

                    return false;
                }

                static bool FocusAwareCollapseItem(TreeViewItem treeViewItem)
                {
                    if (treeViewItem.ItemCount > 0 && treeViewItem.IsExpanded)
                    {
                        if (treeViewItem.IsFocused)
                        {
                            treeViewItem.IsExpanded = false;
                        }
                        else
                        {
                            FocusManager.Instance?.Focus(treeViewItem, NavigationMethod.Directional);
                        }

                        return true;
                    }

                    return false;
                }

                static IEnumerable<TreeViewItem> SubTree(TreeViewItem treeViewItem)
                {
                    return new[] { treeViewItem }.Concat(treeViewItem.LogicalChildren.OfType<TreeViewItem>().SelectMany(child => SubTree(child)));
                }
            }

            // Don't call base.OnKeyDown - let events bubble up to containing TreeView.
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (_header is InputElement previousInputMethod)
            {
                previousInputMethod.DoubleTapped -= HeaderDoubleTapped;
            }

            _header = e.NameScope.Find<Control>("PART_Header");
            _templateApplied = true;

            if (_header is InputElement im)
            {
                im.DoubleTapped += HeaderDoubleTapped;
            }

            if (_deferredBringIntoViewFlag)
            {
                _deferredBringIntoViewFlag = false;
                Dispatcher.UIThread.Post(this.BringIntoView); // must use the Dispatcher, otherwise the TreeView doesn't scroll
            }
        }

        private static int CalculateDistanceFromLogicalParent<T>(ILogical? logical, int @default = -1) where T : class
        {
            var result = 0;

            while (logical != null && !(logical is T))
            {
                ++result;
                logical = logical.LogicalParent;
            }

            return logical != null ? result : @default;
        }

        private void HeaderDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (ItemCount > 0)
            {
                IsExpanded = !IsExpanded;
                e.Handled = true;
            }
        }
    }
}
