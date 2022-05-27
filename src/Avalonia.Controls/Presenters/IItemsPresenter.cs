using System.Collections;
using System.Collections.Specialized;
using Avalonia.Metadata;

namespace Avalonia.Controls.Presenters
{
    [NotClientImplementable]
    public interface IItemsPresenter : IPresenter
    {
        IEnumerable? Items { get; set; }

        IPanel? Panel { get; }

        void ItemsChanged(NotifyCollectionChangedEventArgs e);

        void ScrollIntoView(int index);
    }
}
