using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Displays items in a <see cref="ItemsControl"/> using an <see cref="ItemsRepeater"/>.
    /// </summary>
    public class ItemsRepeaterPresenter : ItemsRepeater, IItemsRepeaterPresenter, IDataTemplate
    {
        private IItemsPresenterHost _host;

        public ItemsRepeaterPresenter()
        {
            ItemTemplate = this;
        }

        static ItemsRepeaterPresenter()
        {
            TemplatedParentProperty.Changed.AddClassHandler<ItemsRepeaterPresenter>(x => x.TemplatedParentChanged);
        }

        public IPanel Panel => this;
        bool IDataTemplate.SupportsRecycling => false;

        public void ScrollIntoView(object item)
        {
            ScrollIntoView(Items.IndexOf(item));
        }

        public bool ScrollIntoView(int index)
        {
            var layoutManager = (VisualRoot as ILayoutRoot)?.LayoutManager;

            if (index >= 0 && index < ItemsSourceView.Count && layoutManager != null)
            {
                var element = GetOrCreateElement(index);

                if (element != null)
                {
                    layoutManager.ExecuteLayoutPass();
                    element.BringIntoView();
                    return true;
                }
            }

            return false;
        }

        public bool TryMoveFocus(NavigationDirection direction)
        {
            var focused = KeyboardNavigation.GetTabOnceActiveElement(this);
            var index = -1;

            if (focused is IControl focusedControl)
            {
                index = GetElementIndex(focusedControl);
            }

            if (index != -1)
            { 
                return direction switch
                {
                    NavigationDirection.Next => MoveFocusTo(index + 1),
                    NavigationDirection.Previous => MoveFocusTo(index - 1),
                    NavigationDirection.First => MoveFocusTo(0),
                    NavigationDirection.Last => MoveFocusTo(ItemsSourceView?.Count - 1 ?? 0),
                    NavigationDirection.Up => TryMoveFocusDirection(index, direction),
                    NavigationDirection.Down => TryMoveFocusDirection(index, direction),
                    NavigationDirection.Left => TryMoveFocusDirection(index, direction),
                    NavigationDirection.Right => TryMoveFocusDirection(index, direction),
                };
            }
            else
            {
                return ScrollIntoView(0);
            }
        }

        bool IDataTemplate.Match(object data) => true;

        IControl ITemplate<object, IControl>.Build(object data)
        {
            if (_host != null)
            {
                var result = _host.CreateContainer(data);
                ((ISetLogicalParent)result)?.SetParent(_host);

                // If the data was the container then prevent recycling. This will be the case
                // when a ListBoxItem appears in a ListBox.Items collection: in this case, the ListBox
                // simply uses the item as the container. However, because the state on this ListBoxItem
                // is set manually there's no easy way to know what that state is, and therefore the item
                // can't take part in virtualization.
                if (result == data)
                {
                    var virtInfo = GetVirtualizationInfo(result);
                    virtInfo.PreventRecycle();
                }

                return result;
            }
            else
            {
                var result = new ContentPresenter();
                result.Bind(
                    ContentPresenter.ContentProperty,
                    result.GetObservable(DataContextProperty));
                return result;
            }
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            var child = ((IVisual)e.Source).GetSelfAndVisualAncestors()
                .FirstOrDefault(x => x.VisualParent == this);

            if (child != null)
            {
                KeyboardNavigation.SetTabOnceActiveElement(this, (IInputElement)child);
            }
        }

        private bool MoveFocusTo(int index)
        {
            if (index >= 0 && index < ItemsSourceView.Count)
            {
                var container = GetOrCreateElement(index);
                FocusManager.Instance?.Focus(container, NavigationMethod.Directional);
                return container != null;
            }

            return false;
        }

        private bool TryMoveFocusDirection(int index, NavigationDirection direction)
        {
            static double Distance(NavigationDirection direction, IInputElement from, IControl to)
            {
                return direction switch
                {
                    NavigationDirection.Left => from.Bounds.Right - to.Bounds.Right,
                    NavigationDirection.Right => to.Bounds.X - from.Bounds.X,
                    NavigationDirection.Up => from.Bounds.Bottom - to.Bounds.Bottom,
                    NavigationDirection.Down => to.Bounds.Y - from.Bounds.Y,
                    _ => double.MaxValue
                };
            }

            var from = TryGetElement(index);

            if (from == null)
            {
                return false;
            }

            IControl result = null;
            var resultDistance = double.MaxValue;

            foreach (var child in Children)
            {
                if (child != from)
                {
                    var distance = Distance(direction, from, child);

                    if (distance > 0 && distance < resultDistance)
                    {
                        result = child;
                        resultDistance = distance;
                    }
                }
            }

            if (result != null)
            {
                MoveFocusTo(GetElementIndex(result));
            }

            return result != null;
        }

        private void TemplatedParentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            _host = e.NewValue as IItemsPresenterHost;
            _host?.RegisterItemsPresenter(this);
        }

        public void ItemsChanged(NotifyCollectionChangedEventArgs e)
        {
        }
    }
}
