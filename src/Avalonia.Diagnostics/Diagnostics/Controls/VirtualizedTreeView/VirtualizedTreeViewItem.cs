using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.Controls.VirtualizedTreeView;

internal sealed class VirtualizedTreeViewItem : ListBoxItem
{
    /// <summary>
    /// Defines the <see cref="IsExpanded"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<VirtualizedTreeViewItem, bool>(
            nameof(IsExpanded),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Defines the <see cref="IndentLevel"/> property.
    /// </summary>
    public static readonly StyledProperty<int> IndentLevelProperty =
        AvaloniaProperty.Register<VirtualizedTreeViewItem, int>(
            nameof(IndentLevel));

    /// <summary>
    /// Defines the <see cref="HasChildren"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> HasChildrenProperty =
        AvaloniaProperty.Register<VirtualizedTreeViewItem, bool>(
            nameof(HasChildren));

    private Control? _presenter;
    private Control? _chevronWithContentPresenter;

    static VirtualizedTreeViewItem()
    {
        RequestBringIntoViewEvent.AddClassHandler<VirtualizedTreeViewItem>((x, e) => x.OnRequestBringIntoView(e));
    }

    /// <summary>
    /// Gets or sets a value indicating whether the item is expanded to show its children.
    /// </summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// Gets or sets the indent level of the item.
    /// </summary>
    public int IndentLevel
    {
        get => GetValue(IndentLevelProperty);
        set => SetValue(IndentLevelProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the item has children.
    /// </summary>
    public bool HasChildren
    {
        get => GetValue(HasChildrenProperty);
        set => SetValue(HasChildrenProperty, value);
    }


    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_presenter is InputElement previousInputMethod)
        {
            previousInputMethod.DoubleTapped -= HeaderDoubleTapped;
        }

        _chevronWithContentPresenter = e.NameScope.Find<Control>("PART_ChevronWithContentPresenter");
        _presenter = e.NameScope.Find<Control>("PART_ContentPresenter");

        if (_presenter is InputElement im)
        {
            im.DoubleTapped += HeaderDoubleTapped;
        }
    }

    private void OnRequestBringIntoView(RequestBringIntoViewEventArgs e)
    {
        if (e.TargetObject == this)
        {
            if (_chevronWithContentPresenter != null)
            {
                var m = _chevronWithContentPresenter.TransformToVisual(this);

                if (m.HasValue)
                {
                    var bounds = new Rect(_chevronWithContentPresenter.Bounds.Size);
                    var rect = bounds.TransformToAABB(m.Value);
                    e.TargetRect = rect;
                }
            }
        }
    }

    private void HeaderDoubleTapped(object? sender, TappedEventArgs e)
    {
        OnHeaderDoubleTapped(e);
    }

    private void OnHeaderDoubleTapped(TappedEventArgs e)
    {
        if (HasChildren)
        {
            SetCurrentValue(IsExpandedProperty, !IsExpanded);
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled)
        {
            var handler =
                e.Key switch
                {
                    Key.Left => ApplyToItemOrRecursivelyIfCtrl(FocusAwareCollapseItem, e.KeyModifiers),
                    Key.Right => ApplyToItemOrRecursivelyIfCtrl(ExpandItem, e.KeyModifiers),
                    Key.Enter => ApplyToItemOrRecursivelyIfCtrl(IsExpanded ? CollapseItem : ExpandItem, e.KeyModifiers),

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
            static Func<VirtualizedTreeViewItem, bool> ApplyToSubtree(Func<VirtualizedTreeViewItem, bool> f)
            {
                // Calling toList enumerates all items before applying functions. This avoids a
                // potential infinite loop if there is an infinite tree (the control catalog is
                // lazily infinite). But also means a lazily loaded tree will not be expanded completely.
                return t => SubTree(t)
                    .ToList()
                    .Select(VirtualizedTreeViewItem => f(VirtualizedTreeViewItem))
                    .Aggregate(false, (p, c) => p || c);
            }

            static Func<VirtualizedTreeViewItem, bool> ApplyToItemOrRecursivelyIfCtrl(Func<VirtualizedTreeViewItem,bool> f, KeyModifiers keyModifiers)
            {
                if (keyModifiers.HasAllFlags(KeyModifiers.Control))
                {
                    return ApplyToSubtree(f);
                }

                return f;
            }

            static bool ExpandItem(VirtualizedTreeViewItem VirtualizedTreeViewItem)
            {
                if (VirtualizedTreeViewItem.HasChildren && !VirtualizedTreeViewItem.IsExpanded)
                {
                    VirtualizedTreeViewItem.SetCurrentValue(IsExpandedProperty, true);
                    return true;
                }

                return false;
            }

            static bool CollapseItem(VirtualizedTreeViewItem VirtualizedTreeViewItem)
            {
                if (VirtualizedTreeViewItem.HasChildren && VirtualizedTreeViewItem.IsExpanded)
                {
                    VirtualizedTreeViewItem.SetCurrentValue(IsExpandedProperty, false);
                    return true;
                }

                return false;
            }

            static bool FocusAwareCollapseItem(VirtualizedTreeViewItem VirtualizedTreeViewItem)
            {
                if (VirtualizedTreeViewItem.HasChildren && VirtualizedTreeViewItem.IsExpanded)
                {
                    if (VirtualizedTreeViewItem.IsFocused)
                    {
                        VirtualizedTreeViewItem.SetCurrentValue(IsExpandedProperty, false);
                    }
                    else
                    {
                        VirtualizedTreeViewItem.Focus(NavigationMethod.Directional);
                    }

                    return true;
                }

                return false;
            }

            static IEnumerable<VirtualizedTreeViewItem> SubTree(VirtualizedTreeViewItem VirtualizedTreeViewItem)
            {
                return new[] { VirtualizedTreeViewItem }.Concat(VirtualizedTreeViewItem.LogicalChildren.OfType<VirtualizedTreeViewItem>().SelectMany(child => SubTree(child)));
            }
        }
    }
}
