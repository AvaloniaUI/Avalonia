using System;
using System.Collections.Specialized;
using Avalonia.Controls.Templates;

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

        private void TemplatedParentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            _host = e.NewValue as IItemsPresenterHost;
            _host?.RegisterItemsPresenter(this);
        }

        public void ItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
