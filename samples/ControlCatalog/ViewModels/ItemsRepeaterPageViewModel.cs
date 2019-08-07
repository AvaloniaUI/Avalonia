using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    public class ItemsRepeaterPageViewModel : ReactiveObject
    {
        private int newItemIndex = 1;

        public ItemsRepeaterPageViewModel()
        {
            Items = new ObservableCollection<string>(
                Enumerable.Range(1, 100000).Select(i => $"Item {i}"));
        }

        public ObservableCollection<string> Items { get; }

        public void AddItem()
        {
            Items.Insert(0, $"New Item {newItemIndex++}");
        }
    }
}
