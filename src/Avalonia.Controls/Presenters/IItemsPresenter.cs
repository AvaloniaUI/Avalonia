using System.Collections;
using System.Collections.Specialized;

namespace Avalonia.Controls.Presenters
{
    public interface IItemsPresenter : IPresenter
    {
        IEnumerable? Items { get; set; }

        IPanel? Panel { get; }

        void ItemsChanged(NotifyCollectionChangedEventArgs e);

        void ScrollIntoView(int index);
    }
}
